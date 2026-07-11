using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Server.Xenoarchaeology.Equipment;

public sealed class PeerReviewConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PeerReviewConsoleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnAfterInteractUsing(
        Entity<PeerReviewConsoleComponent> ent,
        ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ArtifactResearchPrintoutComponent>(
                args.Used,
                out var printout))
        {
            return;
        }

        ent.Comp.StoredValue += printout.Value;

        QueueDel(args.Used);

        _popup.PopupEntity(
            $"Stored research data: {ent.Comp.StoredValue}",
            ent.Owner,
            args.User);

        args.Handled = true;
    }
}
