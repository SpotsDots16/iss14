#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Shitmed;

/// <summary>
/// iss14: end-to-end check of the injection pipeline after the bloodstream solution rework -
/// the injectable solution must resolve on a spawned mob, accept reagents (headroom above the
/// blood fill), and metabolize them into actual effects. Guards against the "can't inject /
/// meds do nothing" report class.
/// </summary>
[TestFixture]
public sealed class ChemInjectionTest : GameTest
{
    private static readonly Robust.Shared.Prototypes.ProtoId<Content.Shared.Damage.Prototypes.DamageTypePrototype> BluntDamage = "Blunt";

    [Test]
    public async Task InjectedTricordrazineMetabolizesAndHeals()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid patient = default;

        await server.WaitAssertion(() =>
        {
            patient = server.EntMan.Spawn("MobHuman", map.MapCoords);

            // The test map is a vacuum: strip pressure damage (Blunt!) and breathing damage so
            // ambient damage cannot mask or fake the medicine's effect.
            server.EntMan.RemoveComponent<Content.Server.Atmos.Components.BarotraumaComponent>(patient);
            server.EntMan.RemoveComponent<Content.Server.Body.Components.RespiratorComponent>(patient);
        });

        await pair.RunTicksSync(5);

        FixedPoint2 damageAfterHit = default;

        await server.WaitAssertion(() =>
        {
            var solutions = server.System<SharedSolutionContainerSystem>();
            var damageable = server.System<DamageableSystem>();

            // Hurt the patient so the heal effect has something to do.
            var blunt = new DamageSpecifier(server.ProtoMan.Index(BluntDamage), 30);
            var dealt = damageable.TryChangeDamage((EntityUid?) patient, blunt,
                ignoreResistances: true,
                canMiss: false);
            Assert.That(dealt, Is.Not.Null.And.Property("Empty").False, "Could not damage the patient.");
            // Track Blunt specifically: the airless test map deals ambient asphyxiation damage,
            // which would pollute a total-damage assertion.
#pragma warning disable CS0618
            damageable.GetAllDamage(patient).DamageDict.TryGetValue("Blunt", out damageAfterHit);
#pragma warning restore CS0618
            Assert.That(damageAfterHit, Is.GreaterThan(FixedPoint2.Zero), "Patient took no blunt damage.");

            // This is the exact capability check the injector/hypospray uses on mobs.
            Assert.That(solutions.TryGetInjectableSolution(patient, out var injectable, out var injectableSolution),
                "TryGetInjectableSolution failed - mobs are not injectable (missing solution or component).");

            Assert.That(injectableSolution!.AvailableVolume, Is.GreaterThan(FixedPoint2.Zero),
                $"Injectable solution has no available volume ({injectableSolution.Volume}/{injectableSolution.MaxVolume}) - " +
                "injectors would report the target as full.");

            // Inject 15u of tricordrazine, like a syringe would.
            var payload = new Solution();
            payload.AddReagent("Tricordrazine", FixedPoint2.New(15));
            Assert.That(solutions.TryAddSolution(injectable.Value, payload),
                "TryAddSolution into the injectable solution failed - injection would do nothing.");

            var injected = injectableSolution.GetTotalPrototypeQuantity("Tricordrazine");
            Assert.That(injected, Is.EqualTo(FixedPoint2.New(15)),
                $"Expected 15u tricordrazine in the bloodstream after injecting, found {injected}.");
        });

        // Let metabolism run. Bloodstream metabolizes at 0.5u/s, so 15 seconds chews through
        // several units and the heal effect must have fired repeatedly.
        await pair.RunSeconds(15);

        await server.WaitAssertion(() =>
        {
            var solutions = server.System<SharedSolutionContainerSystem>();
            var damageable = server.System<DamageableSystem>();

            Assert.That(server.EntMan.TryGetComponent<BloodstreamComponent>(patient, out var bloodstream),
                "Patient lost its bloodstream?");
            Assert.That(solutions.ResolveSolution(patient, bloodstream!.BloodSolutionName,
                    ref bloodstream.BloodSolution, out var bloodSolution),
                "Blood solution did not resolve after injection.");

            var remaining = bloodSolution!.GetTotalPrototypeQuantity("Tricordrazine");
            Assert.That(remaining, Is.LessThan(FixedPoint2.New(15)),
                "Tricordrazine was not metabolized at all - the Bloodstream metabolism stage is not consuming reagents.");

#pragma warning disable CS0618
            damageable.GetAllDamage(patient).DamageDict.TryGetValue("Blunt", out var damageNow);
#pragma warning restore CS0618
            Assert.That(damageNow, Is.LessThan(damageAfterHit),
                $"Blunt damage did not decrease ({damageAfterHit} -> {damageNow}) - " +
                "injected medicine metabolizes but its effects do not apply.");
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(patient));
    }

    /// <summary>
    /// iss14: bisection helper for the injected-meds-don't-heal report. Exercises the two layers
    /// under the reagent effect directly: HealEvenly (what EvenHealthChange calls) and the
    /// part-splitting ChangeDamage heal it delegates to.
    /// </summary>
    [Test]
    public async Task HealEvenlyReducesBodyDamage()
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
            var damageable = server.System<DamageableSystem>();

            var blunt = new DamageSpecifier(server.ProtoMan.Index(BluntDamage), 30);
            damageable.TryChangeDamage((EntityUid?) patient, blunt,
                ignoreResistances: true,
                targetPart: Content.Shared._Shitmed.Targeting.TargetBodyPart.Chest,
                canMiss: false);

#pragma warning disable CS0618
            var before = damageable.GetTotalDamage(patient);
#pragma warning restore CS0618
            Assert.That(before, Is.GreaterThan(FixedPoint2.Zero), "Chest-targeted damage did not land.");

            // Layer 1: the exact call EvenHealthChange makes per metabolism tick (x10 for signal).
            var healed = damageable.HealEvenly((patient, null), FixedPoint2.New(-10), "Brute");

#pragma warning disable CS0618
            var afterHealEvenly = damageable.GetTotalDamage(patient);
#pragma warning restore CS0618
            Assert.That(afterHealEvenly, Is.LessThan(before),
                $"HealEvenly(-10, Brute) did not reduce total damage ({before} -> {afterHealEvenly}); " +
                $"it reported healing: {healed.GetTotal()}.");

            // Layer 2: the split heal HealEvenly delegates to, called directly.
            var healSpec = new DamageSpecifier(server.ProtoMan.Index(BluntDamage), -10);
            damageable.TryChangeDamage((EntityUid?) patient, healSpec,
                ignoreResistances: true,
                targetPart: Content.Shared._Shitmed.Targeting.TargetBodyPart.All,
                splitDamage: Content.Shared._Shitmed.Damage.SplitDamageBehavior.SplitEnsureAllOrganic,
                ignoreBlockers: true,
                canMiss: false);

#pragma warning disable CS0618
            var afterSplitHeal = damageable.GetTotalDamage(patient);
#pragma warning restore CS0618
            Assert.That(afterSplitHeal, Is.LessThan(afterHealEvenly),
                $"Split ChangeDamage heal did not reduce total damage ({afterHealEvenly} -> {afterSplitHeal}).");
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(patient));
    }
}
