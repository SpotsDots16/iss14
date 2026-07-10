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

    /// <summary>
    /// iss14: radiation protection suits must actually reduce radiation damage - the motivating
    /// case of the armor coverage fix (CE hardsuit taking full rads). Irradiates a suited and an
    /// unsuited human exactly the way RadiationSystem does and compares the damage taken.
    /// </summary>
    [Test]
    public async Task RadSuitReducesRadiationDamage()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid unsuited = default;
        EntityUid suited = default;
        EntityUid suit = default;

        await server.WaitAssertion(() =>
        {
            unsuited = server.EntMan.Spawn("MobHuman", map.MapCoords);
            suited = server.EntMan.Spawn("MobHuman", map.MapCoords);
            suit = server.EntMan.Spawn("ClothingOuterSuitRad", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var inventory = server.System<InventorySystem>();
            Assert.That(inventory.TryEquip(suited, suit, "outerClothing", force: true),
                "Could not equip the radiation suit.");
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var damageable = server.System<DamageableSystem>();

            var radsTakenUnsuited = Irradiate(server, unsuited);
            var radsTakenSuited = Irradiate(server, suited);

            Assert.Multiple(() =>
            {
                Assert.That(radsTakenUnsuited, Is.GreaterThan(FixedPoint2.Zero),
                    "Unsuited control took no radiation damage - the irradiation path is broken.");
                // Rad suit: Radiation coefficient 0.01 -> ~1% of the damage.
                Assert.That(radsTakenSuited, Is.LessThan(radsTakenUnsuited / 2),
                    $"Radiation suit did not meaningfully reduce radiation damage " +
                    $"(unsuited {radsTakenUnsuited} vs suited {radsTakenSuited}).");
            });
        });

        await server.WaitPost(() =>
        {
            server.EntMan.DeleteEntity(unsuited);
            server.EntMan.DeleteEntity(suited);
        });
    }

    private static FixedPoint2 Irradiate(Robust.UnitTesting.RobustIntegrationTest.ServerIntegrationInstance server,
        EntityUid target)
    {
        var damageable = server.System<DamageableSystem>();
#pragma warning disable CS0618
        var before = damageable.GetTotalDamage(target);
#pragma warning restore CS0618

        // Exactly how RadiationSystem irradiates receivers: 10 rads over 1 second.
        var ev = new Content.Shared.Radiation.Events.OnIrradiatedEvent(1f, 10f, null);
        server.EntMan.EventBus.RaiseLocalEvent(target, ev);

#pragma warning disable CS0618
        return damageable.GetTotalDamage(target) - before;
#pragma warning restore CS0618
    }
}
