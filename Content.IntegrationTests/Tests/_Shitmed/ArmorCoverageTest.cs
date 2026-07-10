#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Shitmed;

/// <summary>
/// iss14: verifies worn armor actually reduces part-targeted damage. Upstream clothing yaml
/// configures no ArmorCoverage, so before the empty-coverage fallback (pr-5) every vest in the
/// game silently provided zero protection against targeted (i.e. all combat) damage.
/// </summary>
[TestFixture]
public sealed class ArmorCoverageTest : GameTest
{
    private static readonly Robust.Shared.Prototypes.ProtoId<Content.Shared.Damage.Prototypes.DamageTypePrototype> BluntDamage = "Blunt";

    [Test]
    public async Task ArmorVestReducesTargetedDamage()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid unarmored = default;
        EntityUid armored = default;
        EntityUid vest = default;

        await server.WaitAssertion(() =>
        {
            unarmored = server.EntMan.Spawn("MobHuman", map.MapCoords);
            armored = server.EntMan.Spawn("MobHuman", map.MapCoords);
            vest = server.EntMan.Spawn("ClothingOuterArmorBasic", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var inventory = server.System<InventorySystem>();
            Assert.That(inventory.TryEquip(armored, vest, "outerClothing", force: true),
                "Could not equip the armor vest.");
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var damageable = server.System<DamageableSystem>();

            var damageDealtUnarmored = Hit(server, damageable, unarmored);
            var damageDealtArmored = Hit(server, damageable, armored);

            Assert.Multiple(() =>
            {
                Assert.That(damageDealtUnarmored, Is.EqualTo(FixedPoint2.New(10)),
                    $"Unarmored control took {damageDealtUnarmored} of 10 blunt.");
                // Basic vest: Blunt coefficient 0.70 -> 7 damage.
                Assert.That(damageDealtArmored, Is.LessThan(damageDealtUnarmored),
                    $"Armor vest did not reduce targeted damage (unarmored {damageDealtUnarmored} vs armored {damageDealtArmored}) - " +
                    "unconfigured ArmorCoverage is blocking protection again.");
            });
        });

        await server.WaitPost(() =>
        {
            server.EntMan.DeleteEntity(unarmored);
            server.EntMan.DeleteEntity(armored);
        });
    }

    private static FixedPoint2 Hit(Robust.UnitTesting.RobustIntegrationTest.ServerIntegrationInstance server,
        DamageableSystem damageable,
        EntityUid target)
    {
        var blunt = new DamageSpecifier(server.ProtoMan.Index(BluntDamage), 10);
        var dealt = damageable.TryChangeDamage((EntityUid?) target, blunt,
            targetPart: Content.Shared._Shitmed.Targeting.TargetBodyPart.Chest,
            canMiss: false);

        return dealt?.GetTotal() ?? FixedPoint2.Zero;
    }
}
