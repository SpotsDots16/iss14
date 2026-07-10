#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Shitmed.PartStatus;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.HealthExaminable;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Shitmed;

/// <summary>
/// iss14: verifies the context-menu health inspection ("Look for injuries") works end to end -
/// the verb is offered on a spawned human and executing it produces a non-empty part status
/// message. Guards against the "health inspection does nothing" report class.
/// </summary>
[TestFixture]
public sealed class PartStatusExamineTest : GameTest
{
    [Test]
    public async Task HealthExamineVerbProducesPartStatus()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid patient = default;
        EntityUid examiner = default;

        await server.WaitAssertion(() =>
        {
            patient = server.EntMan.Spawn("MobHuman", map.MapCoords);
            examiner = server.EntMan.Spawn("MobHuman", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var verbSystem = server.System<SharedVerbSystem>();
            var verbs = verbSystem.GetLocalVerbs(patient, examiner, typeof(ExamineVerb), force: true);

            // "health-examinable-verb-text = Health" in en-US locale (Loc is unavailable in tests).
            var healthVerb = verbs.FirstOrDefault(v => v.Text == "Health");

            Assert.That(healthVerb, Is.Not.Null,
                "No health inspection verb was offered on a spawned human - " +
                "PartStatusSystem's GetVerbsEvent subscription is not producing the verb.");

            // Execute the same code path the verb Act runs, but capture the message directly
            // so we can assert on its contents.
            var partStatus = server.System<PartStatusSystem>();
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.TryGetComponent<HealthExaminableComponent>(patient, out var examinable),
                    "Spawned human has no HealthExaminableComponent.");
                Assert.That(server.EntMan.TryGetComponent<DamageableComponent>(patient, out var damageable),
                    "Spawned human has no DamageableComponent.");

                var markup = partStatus.CreateMarkup(patient, examiner, examinable!, damageable!);
                Assert.That(markup.IsEmpty, Is.False,
                    "CreateMarkup returned an empty message - TryGetRootPart or part collection failed.");

                var text = markup.ToString();
                Assert.That(text, Does.Contain("head").IgnoreCase,
                    $"Part status message does not mention the head. Message was: \"{text}\"");
            });

            // And run the actual verb Act to prove it does not throw.
            Assert.That(() => healthVerb!.Act?.Invoke(), Throws.Nothing,
                "Executing the health inspection verb threw an exception.");
        });

        await server.WaitPost(() =>
        {
            server.EntMan.DeleteEntity(patient);
            server.EntMan.DeleteEntity(examiner);
        });
    }
}
