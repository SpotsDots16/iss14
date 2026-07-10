using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared._Shitmed.Targeting; // Goobstation - ranged aim-miss falloff
using Robust.Shared.Random; // Goobstation - ranged aim-miss falloff

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IRobustRandom _random = default!; // Goobstation - ranged aim-miss falloff
    [Dependency] private SharedTransformSystem _transform = default!; // Goobstation - ranged aim-miss falloff

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // Goobstation - ranged aim-miss falloff: the aimed body part only lands close-up; the
        // chance falls off with distance to the victim and the hit degrades to the chest.
        TargetBodyPart? targetPart = null;
        if (args.Data.Shooter is { } shooter && HasComp<TargetingComponent>(shooter))
        {
            var shooterCoords = _transform.GetMapCoordinates(shooter);
            var targetCoords = _transform.GetMapCoordinates(args.Data.HitEntity.Value);
            var missChance = shooterCoords.MapId != targetCoords.MapId
                ? 1f
                : Math.Clamp((shooterCoords.Position - targetCoords.Position).Length() / 2f, 0f, 1f);

            if (_random.Prob(missChance))
                targetPart = TargetBodyPart.Chest;
        }

        // Shitmed Change: origin must be the shooter so damage routes to their targeted body part
        // (the gun has no TargetingComponent - the targeting doll did nothing for hitscan weapons)
        if(!_damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out var damageDealt, origin: args.Data.Shooter ?? args.Data.Gun, targetPart: targetPart))
            return;

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
        };

        RaiseLocalEvent(ent, ref damageEvent);
    }
}
