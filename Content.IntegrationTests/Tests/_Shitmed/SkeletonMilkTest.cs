#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Shitmed;

/// <summary>
/// iss14: skeletons are healed by splashing milk on them (upstream Reactive block on the
/// species). The heal is an EvenHealthChange routed through the organic-part split, which
/// zeroed it for all-inorganic skeleton bodies - keep the fallback guarded.
/// </summary>
[TestFixture]
public sealed class SkeletonMilkTest : GameTest
{
    private static readonly Robust.Shared.Prototypes.ProtoId<Content.Shared.Damage.Prototypes.DamageTypePrototype> BluntDamage = "Blunt";

    [Test]
    public async Task MilkSplashHealsSkeleton()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid skeleton = default;

        await server.WaitAssertion(() =>
        {
            skeleton = server.EntMan.Spawn("MobSkeletonPerson", map.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var damageable = server.System<DamageableSystem>();
            var reactive = server.System<ReactiveSystem>();

            var blunt = new DamageSpecifier(server.ProtoMan.Index(BluntDamage), 20);
            damageable.TryChangeDamage((EntityUid?) skeleton, blunt,
                ignoreResistances: true,
                canMiss: false);

#pragma warning disable CS0618
            damageable.GetAllDamage(skeleton).DamageDict.TryGetValue("Blunt", out var before);
#pragma warning restore CS0618
            Assert.That(before, Is.GreaterThan(FixedPoint2.Zero), "Skeleton took no blunt damage.");

            // Splash 10u of milk, exactly how spills/vapor touch reactions do it.
            var milk = new Solution();
            milk.AddReagent("Milk", FixedPoint2.New(10));
            reactive.DoEntityReaction(skeleton, milk, ReactionMethod.Touch);

#pragma warning disable CS0618
            damageable.GetAllDamage(skeleton).DamageDict.TryGetValue("Blunt", out var after);
#pragma warning restore CS0618
            Assert.That(after, Is.LessThan(before),
                $"Splashing milk did not heal the skeleton ({before} -> {after}) - " +
                "the organic-part split is zeroing heals on all-inorganic bodies again.");
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(skeleton));
    }
}
