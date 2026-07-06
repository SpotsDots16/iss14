#nullable enable
using Content.Goobstation.Common.Grab;
using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.GrabIntent;
using Content.Goobstation.Shared.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Goobstation;

/// <summary>
/// iss14: verifies the ported Goobstation grab system and martial arts grant path.
/// </summary>
[TestFixture]
public sealed class GrabMartialArtsTest : GameTest
{
    /// <summary>
    /// Starting a pull in combat mode must escalate the grab stage (No -> Soft),
    /// and a follow-up grab (after the stage-change cooldown) must escalate further (Soft -> Hard).
    /// </summary>
    [Test]
    public async Task GrabEscalatesStages()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid attacker = default;
        EntityUid victim = default;

        await server.WaitAssertion(() =>
        {
            attacker = server.EntMan.Spawn("MobHuman", map.MapCoords);
            victim = server.EntMan.Spawn("MobHuman", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<GrabIntentComponent>(attacker),
                    "Spawned human has no GrabIntentComponent - species base prototype is missing it.");
                Assert.That(server.EntMan.HasComponent<GrabbableComponent>(victim),
                    "Spawned human has no GrabbableComponent - species base prototype is missing it.");
            });

            var combatMode = server.System<Content.Shared.CombatMode.SharedCombatModeSystem>();
            combatMode.SetInCombatMode(attacker, true);

            var pulling = server.System<Content.Shared.Movement.Pulling.Systems.PullingSystem>();
            Assert.That(pulling.TryStartPull(attacker, victim), "Attacker could not start pulling the victim.");

            var grabbable = server.EntMan.GetComponent<GrabbableComponent>(victim);
            Assert.That(grabbable.GrabStage, Is.EqualTo(GrabStage.Soft),
                "Starting a pull in combat mode did not escalate the grab to Soft.");
        });

        // Wait out the grab stage-change cooldown (1 second) plus some slack.
        await pair.RunTicksSync(120);

        await server.WaitAssertion(() =>
        {
            var grab = server.System<GrabIntentSystem>();
            Assert.That(grab.TryGrab(victim, attacker), "Second grab attempt failed outright.");

            var grabbable = server.EntMan.GetComponent<GrabbableComponent>(victim);
            Assert.That(grabbable.GrabStage, Is.EqualTo(GrabStage.Hard),
                "Second grab did not escalate the stage from Soft to Hard.");

            var grabIntent = server.EntMan.GetComponent<GrabIntentComponent>(attacker);
            Assert.That(grabIntent.GrabStage, Is.EqualTo(GrabStage.Hard),
                "Puller grab intent stage did not track the grab escalation.");
        });

        await server.WaitPost(() =>
        {
            server.EntMan.DeleteEntity(attacker);
            server.EntMan.DeleteEntity(victim);
        });
    }

    /// <summary>
    /// Granting CQC through the martial arts grant API must land both
    /// MartialArtsKnowledgeComponent and CanPerformComboComponent on the mob,
    /// with the CQC combo list loaded.
    /// </summary>
    [Test]
    public async Task GrantCqcAddsKnowledge()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid student = default;

        await server.WaitAssertion(() =>
        {
            student = server.EntMan.Spawn("MobHuman", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var martialArts = server.System<SharedMartialArtsSystem>();
            var grant = new GrantCqcComponent();

            Assert.That(martialArts.TryGrantMartialArt(student, grant),
                "TryGrantMartialArt failed to grant CQC to a fresh human.");

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.TryGetComponent<MartialArtsKnowledgeComponent>(student, out var knowledge),
                    "Granted mob has no MartialArtsKnowledgeComponent.");
                Assert.That(knowledge!.MartialArtsForm, Is.EqualTo(MartialArtsForms.CloseQuartersCombat),
                    "Granted knowledge is not CQC.");

                Assert.That(server.EntMan.TryGetComponent<CanPerformComboComponent>(student, out var combo),
                    "Granted mob has no CanPerformComboComponent.");
                Assert.That(combo!.AllowedCombos, Is.Not.Empty,
                    "CQC combo list was not loaded onto CanPerformComboComponent.");
            });
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(student));
    }

    /// <summary>
    /// The CQC manual item must spawn and carry the granting component.
    /// </summary>
    [Test]
    public async Task GrantingItemsSpawn()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var manual = server.EntMan.Spawn("CQCManual", map.MapCoords);
            Assert.That(server.EntMan.HasComponent<GrantCqcComponent>(manual),
                "CQCManual has no GrantCqcComponent.");
            server.EntMan.DeleteEntity(manual);

            var scroll = server.EntMan.Spawn("SleepingCarpScroll", map.MapCoords);
            Assert.That(server.EntMan.HasComponent<GrantSleepingCarpComponent>(scroll),
                "SleepingCarpScroll has no GrantSleepingCarpComponent.");
            server.EntMan.DeleteEntity(scroll);
        });
    }
}
