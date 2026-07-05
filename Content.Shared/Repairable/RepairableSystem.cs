using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;
// Shitmed Change Start
using System.Linq;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Healing;
// Shitmed Change End

namespace Content.Shared.Repairable;

public sealed partial class RepairableSystem : EntitySystem
{
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedBodySystem _bodySystem = default!;          // Goob edit
    [Dependency] private HealingSystem _healingSystem = default!;          // Goob edit
    [Dependency] private WoundSystem _wounds = default!;                   // Goob edit

    public override void Initialize()
    {
        SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        SubscribeLocalEvent<RepairableComponent, RepairDoAfterEvent>(OnRepairDoAfter);
    }

    private void OnRepairDoAfter(Entity<RepairableComponent> ent, ref RepairDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(ent.Owner, out DamageableComponent? damageable))
            return;

        var totalDamage = _damageableSystem.GetTotalDamage((ent.Owner, damageable));
        if (totalDamage == 0)
            return;

        // Shitmed Change Start - repair entities with bodies (e.g. IPCs) limb by limb via the wound system
        if (HasComp<BodyComponent>(ent.Owner) && ent.Comp.Damage != null)
        {
            args.Repeat = ApplyRepairs(ent, args.User);
            args.Args.Event.Repeat = args.Repeat;
            args.Handled = true;

            if (!args.Repeat)
            {
                var str = Loc.GetString("comp-repairable-repair", ("target", ent.Owner), ("tool", args.Used!));
                _popup.PopupClient(str, ent.Owner, args.User);

                var ev = new RepairedEvent(ent, args.User);
                RaiseLocalEvent(ent.Owner, ref ev);
            }

            return;
        }
        // Shitmed Change End

        if (ent.Comp.DamageValue != null)
            RepairSomeDamage((ent, damageable), ent.Comp.DamageValue.Value, args.User);
        else if (ent.Comp.Damage != null)
            RepairSomeDamage((ent, damageable), ent.Comp.Damage, args.User);
        else
            RepairAllDamage((ent, damageable), args.User);

        totalDamage = _damageableSystem.GetTotalDamage((ent.Owner, damageable));

        args.Repeat = ent.Comp.AutoDoAfter && totalDamage > 0;
        args.Args.Event.Repeat = args.Repeat;
        args.Handled = true;

        if (!args.Repeat)
        {
            var str = Loc.GetString("comp-repairable-repair", ("target", ent.Owner), ("tool", args.Used!));
            _popup.PopupClient(str, ent.Owner, args.User);

            var ev = new RepairedEvent(ent, args.User);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    /// <summary>
    /// Repairs some damage of a entity.
    /// The healed amount will be evenly distributed among all damage types the entity has.
    /// If one of the damage types of the entity is too low. it will heal that completly and distribute the excess healing among the other damage types
    /// </summary>
    /// <param name="ent">entity to be repaired</param>
    /// <param name="damageAmount">how much damage to repair (value have to be negative to repair)</param>
    /// <param name="user">who is doing the repair</param>
    private void RepairSomeDamage(Entity<DamageableComponent?> ent, float damageAmount, EntityUid user)
    {
        var damageChanged = _damageableSystem.HealEvenly(ent.Owner, damageAmount, origin: user);
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} by {damageChanged.GetTotal()}");
    }

    /// <summary>
    /// Repairs some damage of a entity
    /// </summary>
    /// <param name="ent">entity to be repaired</param>
    /// <param name="damageAmount">how much damage to repair (values have to be negative to repair)</param>
    /// <param name="user">who is doing the repair</param>
    private void RepairSomeDamage(Entity<DamageableComponent?> ent, Damage.DamageSpecifier damageAmount, EntityUid user)
    {
        var damageChanged = _damageableSystem.ChangeDamage(ent.Owner, damageAmount, true, false, origin: user);
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} by {damageChanged.GetTotal()}");
    }

    /// <summary>
    /// Repairs all damage of a entity
    /// </summary>
    /// <param name="ent">entity to be repaired</param>
    /// <param name="user">who is doing the repair</param>
    private void RepairAllDamage(Entity<DamageableComponent?> ent, EntityUid user)
    {
        _damageableSystem.ClearAllDamage(ent);
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} back to full health");
    }

    // Shitmed Change Start (Goob edit)
    /// <summary>
    /// Method <c>ApplyRepairs</c> Applies repair according to "RepairableComponent" present on entity. Returns false if fail or nothing else to repair.
    /// </summary>
    /// <param name="ent">the target Entity</param>
    /// <param name="user">The entity trying to repair</param>
    /// <returns> Whether or not there is something else to repair. If fails, returns false too </returns>
    public bool ApplyRepairs(Entity<RepairableComponent> ent, EntityUid user)
    {
        if (!TryComp(ent.Owner, out DamageableComponent? damageable)
            || _damageableSystem.GetTotalDamage((ent.Owner, damageable)) == 0)
            return false;

        if (user == ent.Owner)
            if (!ent.Comp.AllowSelfRepair)
                return false;

        if (TryComp<BodyComponent>(ent.Owner, out var body) && ent.Comp.Damage != null && body != null) // repair entities with bodies
        {
            // here we create a fake healing comp
            var repairHealing = new HealingComponent();
            repairHealing.Damage = ent.Comp.Damage;
            repairHealing.BloodlossModifier = -100;

            var targetedWoundable = EntityUid.Invalid;
            if (TryComp<TargetingComponent>(user, out var targeting))
            {
                var (partType, symmetry) = _bodySystem.ConvertTargetBodyPart(targeting.Target);
                var targetedBodyPart = _bodySystem.GetBodyChildrenOfType(ent, partType, body, symmetry).ToList().FirstOrDefault();
                targetedWoundable = targetedBodyPart.Id;
            }
            else
            {
                if (_healingSystem.TryGetNextDamagedPart(ent, repairHealing, out var limbTemp) && limbTemp is not null)
                    targetedWoundable = limbTemp.Value;
            }

            if (!TryComp<DamageableComponent>(targetedWoundable, out var damageableComp))
                return false;

            if (!_healingSystem.IsBodyDamaged((ent.Owner, body), null, repairHealing, targetedWoundable))                    // Check if there is anything to heal on the initial limb target
                if (_healingSystem.TryGetNextDamagedPart(ent, repairHealing, out var limbTemp) && limbTemp is not null)      // If not then get the next limb to heal
                    targetedWoundable = limbTemp.Value;

            // Welding removes all bleeding instantly. IPC don't even have blood as i'm writing this so makes 0 sense for them to have bleeds.
            if (TryComp<WoundableComponent>(targetedWoundable, out var woundableComp))
            {
                var healedBleedWound = _wounds.TryHealBleedingWounds(targetedWoundable, repairHealing.BloodlossModifier, out FixedPoint2 modifiedBleedStopAbility, woundableComp);
                if (healedBleedWound)
                    _popup.PopupPredicted(modifiedBleedStopAbility > 0
                            ? Loc.GetString("rebell-medical-item-stop-bleeding-fully")
                            : Loc.GetString("rebell-medical-item-stop-bleeding-partially"),
                        ent,
                        user);
            }

            var damageChanged = _damageableSystem.ChangeDamage(targetedWoundable, ent.Comp.Damage, true, false, origin: user);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} by {damageChanged.GetTotal()}");

            if (_healingSystem.TryGetNextDamagedPart(ent.Owner, repairHealing, out _))
                return true;
        }
        else if (ent.Comp.Damage != null)
        {
            RepairSomeDamage((ent.Owner, damageable), ent.Comp.Damage, user);
        }
        else
        {
            // Repair all damage
            RepairAllDamage((ent.Owner, damageable), user);
        }

        return false;
    }
    // Shitmed Change End (Goob edit)

    private void Repair(Entity<RepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Only try repair the target if it is damaged
        if (_damageableSystem.GetTotalDamage(ent.Owner) == 0)
            return;

        // Shitmed Change Start (Goob edit) - if there is nothing to heal on a body, don't try it
        if (TryComp<BodyComponent>(ent.Owner, out var bodyComp) && ent.Comp.Damage != null)
        {
            var repairHealing = new HealingComponent();
            repairHealing.Damage = ent.Comp.Damage;
            repairHealing.BloodlossModifier = -100;
            if (!_healingSystem.TryGetNextDamagedPart(ent.Owner, repairHealing, out _))
                return;
        }
        // Shitmed Change End

        float delay = ent.Comp.DoAfterDelay;

        // Add a penalty to how long it takes if the user is repairing itself
        if (args.User == args.Target)
        {
            if (!ent.Comp.AllowSelfRepair)
                return;

            delay *= ent.Comp.SelfRepairPenalty;
        }

        // Run the repairing doafter
        args.Handled = _toolSystem.UseTool(args.Used, args.User, ent.Owner, delay, ent.Comp.QualityNeeded, new RepairDoAfterEvent(), ent.Comp.FuelCost);
    }
}

/// <summary>
/// Event raised on an entity when its successfully repaired.
/// </summary>
/// <param name="Ent"></param>
/// <param name="User"></param>
[ByRefEvent]
public readonly record struct RepairedEvent(Entity<RepairableComponent> Ent, EntityUid User);

/// <summary>
/// Do after event started when you try to fix a entity with RepairableComponent.
/// This doafter is repeated if the entity has <see cref="AutoDoAfter"> set to true and not all damage was fixed yet.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RepairDoAfterEvent : SimpleDoAfterEvent;
