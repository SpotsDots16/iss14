#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Shitmed.Medical.Surgery;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Shitmed;

/// <summary>
/// iss14: verifies the Shitmed surgery entry points exist on a spawned human -
/// the SurgeryTargetComponent (added at body MapInit) and the SurgeryBui UI registration.
/// Guards against regressions of the "scalpel does nothing" class.
/// </summary>
[TestFixture]
public sealed class SurgeryTargetTest : GameTest
{
    [Test]
    public async Task HumanIsSurgeryTarget()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid human = default;
        await server.WaitAssertion(() => human = server.EntMan.Spawn("MobHuman", map.MapCoords));

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var uiSystem = server.System<SharedUserInterfaceSystem>();
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<SurgeryTargetComponent>(human),
                    "Spawned human has no SurgeryTargetComponent - body MapInit EnsureComp did not run.");

                Assert.That(uiSystem.HasUi(human, SurgeryUIKey.Key),
                    "Spawned human has no SurgeryBui user interface registered for SurgeryUIKey.");
            });
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(human));
    }

    /// <summary>
    /// iss14: end-to-end check that choosing a surgery step actually starts the do-after
    /// ("clicking a step does nothing" regression). Calls TryDoSurgeryStep directly and
    /// surfaces the exact validation gate on failure.
    /// </summary>
    [Test]
    public async Task ScalpelStepStartsDoAfter()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid patient = default;
        EntityUid surgeon = default;
        EntityUid scalpel = default;

        await server.WaitAssertion(() =>
        {
            patient = server.EntMan.Spawn("MobHuman", map.MapCoords);
            surgeon = server.EntMan.Spawn("MobHuman", map.MapCoords);
            scalpel = server.EntMan.Spawn("Scalpel", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var hands = server.System<Content.Shared.Hands.EntitySystems.SharedHandsSystem>();
            Assert.That(hands.TryPickupAnyHand(surgeon, scalpel), "Surgeon could not pick up the scalpel.");

            // Patient must be lying down for surgery.
            var standing = server.System<Content.Shared.Standing.StandingStateSystem>();
            standing.Down(patient);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var surgery = server.System<Content.Shared._Shitmed.Medical.Surgery.SharedSurgerySystem>();
            var body = server.System<Content.Shared.Body.Systems.SharedBodySystem>();

            // Find the chest part to operate on.
            var chest = body.GetBodyChildrenOfType(patient, Content.Shared.Body.Part.BodyPartType.Chest)
                .FirstOrNull();
            Assert.That(chest, Is.Not.Null, "Patient has no chest body part.");

            var started = surgery.TryDoSurgeryStep(
                patient,
                chest!.Value.Id,
                surgeon,
                "SurgeryOpenIncision",
                "SurgeryStepOpenIncisionScalpel",
                out var error);

            Assert.That(started, $"TryDoSurgeryStep failed with StepInvalidReason.{error}");
        });

        await server.WaitPost(() =>
        {
            server.EntMan.DeleteEntity(patient);
            server.EntMan.DeleteEntity(surgeon);
        });
    }

    /// <summary>
    /// iss14: verifies part-targeted damage creates wounds on the targeted body part
    /// (prerequisite for dismemberment; "head doesn't come off" report).
    /// </summary>
    [Test]
    public async Task TargetedDamageWoundsPart()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid patient = default;

        await server.WaitAssertion(() =>
        {
            patient = server.EntMan.Spawn("MobHuman", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var body = server.System<Content.Shared.Body.Systems.SharedBodySystem>();
            var damageable = server.System<Content.Shared.Damage.Systems.DamageableSystem>();
            var wounds = server.System<Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems.WoundSystem>();

            var head = body.GetBodyChildrenOfType(patient, Content.Shared.Body.Part.BodyPartType.Head)
                .FirstOrNull();
            Assert.That(head, Is.Not.Null, "Patient has no head.");

            var slash = new Content.Shared.Damage.DamageSpecifier(
                server.ProtoMan.Index<Content.Shared.Damage.Prototypes.DamageTypePrototype>("Slash"), 30);

            var dealt = damageable.TryChangeDamage((EntityUid?) patient, slash,
                ignoreResistances: true,
                targetPart: Content.Shared._Shitmed.Targeting.TargetBodyPart.Head,
                canMiss: false);

            Assert.That(dealt, Is.Not.Null.And.Property("Empty").False,
                "Part-targeted damage was dropped entirely.");

            var headWounds = wounds.GetWoundableWounds(head!.Value.Id).ToList();
            Assert.That(headWounds, Is.Not.Empty,
                "Damage to the head created no wounds - the damage->wound routing is broken.");

            // Now exercise the dismemberment machinery itself. The damage->dismemberment
            // trigger is a probability roll (TraumaSystem.RandomDismembermentTraumaChance),
            // so we invoke the deterministic endpoint the roll leads to: AmputateWoundable.
            var woundable = server.EntMan.GetComponent<
                Content.Shared._Shitmed.Medical.Surgery.Wounds.Components.WoundableComponent>(head.Value.Id);
            Assert.That(woundable.ParentWoundable, Is.Not.Null, "Head woundable has no parent woundable.");

            wounds.AmputateWoundable(woundable.ParentWoundable!.Value, head.Value.Id, woundable);
        });

        await pair.RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            var body = server.System<Content.Shared.Body.Systems.SharedBodySystem>();
            var stillAttached = body.GetBodyChildrenOfType(patient, Content.Shared.Body.Part.BodyPartType.Head)
                .FirstOrNull();
            Assert.That(stillAttached, Is.Null,
                "AmputateWoundable did not detach the head - the dismemberment machinery is broken.");
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(patient));
    }
}
