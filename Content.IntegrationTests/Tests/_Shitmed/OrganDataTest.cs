#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Shitmed;

/// <summary>
/// iss14: verifies the health analyzer's organ data source works for every playable species -
/// GetBodyOrgans must return the species' organs ("slime person has no organs overview" report).
/// </summary>
[TestFixture]
public sealed class OrganDataTest : GameTest
{
    [Test]
    [TestCase("MobHuman", 5)]
    [TestCase("MobSlimePerson", 2)]
    [TestCase("MobMoth", 5)]
    [TestCase("MobArachnid", 5)]
    [TestCase("MobVulpkanin", 5)]
    public async Task SpeciesHasOrgans(string prototype, int minOrgans)
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid mob = default;
        await server.WaitAssertion(() => mob = server.EntMan.Spawn(prototype, map.MapCoords));

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var body = server.System<Content.Shared.Body.Systems.SharedBodySystem>();
            var organs = body.GetBodyOrgans(mob).ToList();

            Assert.That(organs, Has.Count.GreaterThanOrEqualTo(minOrgans),
                $"{prototype} has {organs.Count} organs, expected at least {minOrgans} - " +
                $"found: [{string.Join(", ", organs.Select(o => server.EntMan.ToPrettyString(o.Id)))}] - " +
                "the health analyzer organs tab would be empty.");

            // The analyzer skips organs whose IntegrityCap is 0, so make sure none are invisible.
            foreach (var (organId, organComp) in organs)
            {
                Assert.That(organComp.IntegrityCap, Is.GreaterThan(Content.Shared.FixedPoint.FixedPoint2.Zero),
                    $"{prototype} organ {server.EntMan.ToPrettyString(organId)} has IntegrityCap 0 - " +
                    "the health analyzer would silently skip it.");
            }
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(mob));
    }
}
