using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Server.Xenoarchaeology.Equipment;

public sealed partial class PeerReviewConsoleSystem : EntitySystem
{
    private const int SmallPublicationCost = 4;
    private const int MediumPublicationCost = 8;
    private const int LargePublicationCost = 16;

    private const string SmallResearchDiskPrototype = "ResearchDisk";
    private const string MediumResearchDiskPrototype = "ResearchDisk5000";
    private const string LargeResearchDiskPrototype = "ResearchDisk10000";

    private enum PublicationTier
    {
        Small,
        Medium,
        Large,
    }

    [Dependency] private SharedPopupSystem _popup = default!;

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
    private void TryPublish(
    Entity<PeerReviewConsoleComponent> ent,
    EntityUid user,
    PublicationTier tier)
    {
        var (cost, diskPrototype) = tier switch
        {
            PublicationTier.Small =>
                (SmallPublicationCost, SmallResearchDiskPrototype),

            PublicationTier.Medium =>
                (MediumPublicationCost, MediumResearchDiskPrototype),

            PublicationTier.Large =>
                (LargePublicationCost, LargeResearchDiskPrototype),

            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
        };

        if (ent.Comp.StoredValue < cost)
        {
            _popup.PopupEntity(
                $"Not enough research data. Required: {cost}.",
                ent.Owner,
                user);

            return;
        }

        ent.Comp.StoredValue -= cost;

        Spawn(diskPrototype, Transform(ent.Owner).Coordinates);

        _popup.PopupEntity(
            $"Publication completed. Remaining data: {ent.Comp.StoredValue}.",
            ent.Owner,
            user);
    }

}
