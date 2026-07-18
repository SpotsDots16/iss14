#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Carpmosia;

/// <summary>
/// iss14: the Tajaran species is ported from Carpmosia's organ-based body format to this
/// fork's classic + Shitmed part-based format. Make sure the translated body tree actually
/// assembles on spawn and the humanoid appearance resolves.
/// </summary>
[TestFixture]
public sealed class TajaranSpawnTest : GameTest
{
    [Test]
    public async Task TajaranSpawnsWithBody()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid tajaran = default;

        await server.WaitAssertion(() =>
        {
            tajaran = server.EntMan.Spawn("MobTajaran", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.TryGetComponent<BodyComponent>(tajaran, out _),
                "Spawned Tajaran has no body component.");

            var bodySystem = server.System<SharedBodySystem>();
            var parts = bodySystem.GetBodyChildren(tajaran).ToList();
            Assert.That(parts, Has.Count.GreaterThanOrEqualTo(11),
                $"Tajaran body assembled only {parts.Count} parts - the part-based body tree is broken.");

            Assert.That(server.EntMan.TryGetComponent<HumanoidAppearanceComponent>(tajaran, out var humanoid),
                "Spawned Tajaran has no humanoid appearance.");
            Assert.That(humanoid!.Species.Id, Is.EqualTo("Tajaran"));
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(tajaran));
    }
}
