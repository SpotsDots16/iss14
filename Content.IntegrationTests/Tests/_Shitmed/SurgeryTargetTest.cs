#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Shitmed.Medical.Surgery;
using Robust.Shared.GameObjects;

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
}
