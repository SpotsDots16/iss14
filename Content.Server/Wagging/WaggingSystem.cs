using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Content.Shared.Wagging;
using Robust.Shared.Prototypes;

namespace Content.Server.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this
/// </summary>
public sealed partial class WaggingSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnWaggingMapInit);
        SubscribeLocalEvent<WaggingComponent, ComponentShutdown>(OnWaggingShutdown);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnWaggingToggle);
        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WaggingComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        // Make sure to set the datafields before adding the component so that the correct action gets spawned on map init.
        var cloneComp = Factory.GetComponent<WaggingComponent>();
        cloneComp.Action = ent.Comp.Action;
        cloneComp.Layer = ent.Comp.Layer;
        cloneComp.Suffix = ent.Comp.Suffix;
        AddComp(args.CloneUid, cloneComp, true);
    }

    private void OnWaggingMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
    }

    private void OnWaggingShutdown(Entity<WaggingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnWaggingToggle(Entity<WaggingComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleWagging(ent.AsNullable());
    }

    private void OnMobStateChanged(Entity<WaggingComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Wagging)
            TryToggleWagging(ent.AsNullable());
    }

    public bool TryToggleWagging(Entity<WaggingComponent?> ent, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(ent, ref humanoid))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            return false;

        if (markings.Count == 0)
            return false;

        ent.Comp.Wagging = !ent.Comp.Wagging;

        for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails
        {
            var currentMarkingId = markings[idx].MarkingId;
            string newMarkingId;

            if (ent.Comp.Wagging)
            {
                newMarkingId = $"{currentMarkingId}{ent.Comp.Suffix}";
            }
            else
            {
                if (currentMarkingId.EndsWith(ent.Comp.Suffix))
                {
                    newMarkingId = currentMarkingId[..^ent.Comp.Suffix.Length];
                }
                else
                {
                    newMarkingId = currentMarkingId;
                    Log.Warning($"Unable to revert wagging for {currentMarkingId}");
                }
            }

            if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
            {
                Log.Warning($"{ToPrettyString(ent):ent} tried toggling wagging but {newMarkingId} marking doesn't exist");
                continue;
            }

            _humanoidAppearance.SetMarkingId(ent, MarkingCategories.Tail, idx, newMarkingId,
                humanoid: humanoid);
        }

        return true;
    }
}
