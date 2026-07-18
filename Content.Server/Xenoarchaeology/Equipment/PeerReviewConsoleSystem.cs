using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.UserInterface;
using Content.Shared.Xenoarchaeology.Equipment;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;

namespace Content.Server.Xenoarchaeology.Equipment;

public sealed partial class PeerReviewConsoleSystem : EntitySystem
{
    private const string SmallResearchDiskPrototype = "ResearchDisk";
    private const string MediumResearchDiskPrototype = "ResearchDisk5000";
    private const string LargeResearchDiskPrototype = "ResearchDisk10000";

    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PeerReviewConsoleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<PeerReviewConsoleComponent, AfterActivatableUIOpenEvent>(OnUiOpened);
        SubscribeLocalEvent<PeerReviewConsoleComponent, PeerReviewConsolePublishMessage>(OnPublishMessage);
    }

    private void OnAfterInteractUsing(
        Entity<PeerReviewConsoleComponent> ent,
        ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        // Only care about clicks with a research printout in hand.
        if (!TryComp<ArtifactResearchPrintoutComponent>(
                args.Used,
                out var printout))
        {
            return;
        }

        if (!IsPowered(ent.Owner))
        {
            _popup.PopupEntity(
                Loc.GetString("peer-review-console-unpowered"),
                ent.Owner,
                args.User);

            args.Handled = true;
            return;
        }

        if (!HasConsoleAccess(args.User, ent.Owner))
        {
            _popup.PopupEntity(
                Loc.GetString("peer-review-console-access-denied"),
                ent.Owner,
                args.User);

            args.Handled = true;
            return;
        }

        ent.Comp.StoredValue += printout.Value;
        _audio.PlayPvs(ent.Comp.InsertSound, ent.Owner);

        QueueDel(args.Used);
        UpdateUi(ent);

        _popup.PopupEntity(
            Loc.GetString("peer-review-console-stored", ("value", ent.Comp.StoredValue)),
            ent.Owner,
            args.User);

        args.Handled = true;
    }

    private void OnUiOpened(
        Entity<PeerReviewConsoleComponent> ent,
        ref AfterActivatableUIOpenEvent args)
    {
        _audio.PlayPvs(ent.Comp.KeyboardSound, ent.Owner);
        UpdateUi(ent);
    }

    private void OnPublishMessage(
        Entity<PeerReviewConsoleComponent> ent,
        ref PeerReviewConsolePublishMessage args)
    {
        TryPublish(ent, args.Actor, args.Tier);
    }

    private void TryPublish(
        Entity<PeerReviewConsoleComponent> ent,
        EntityUid user,
        PublicationTier tier)
    {
        if (!IsPowered(ent.Owner))
            return;

        if (!HasConsoleAccess(user, ent.Owner))
        {
            _popup.PopupEntity(
                Loc.GetString("peer-review-console-access-denied"),
                ent.Owner,
                user);

            return;
        }

        var (cost, diskPrototype) = tier switch
        {
            PublicationTier.Small =>
                (PeerReviewConsoleConstants.SmallPublicationCost, SmallResearchDiskPrototype),

            PublicationTier.Medium =>
                (PeerReviewConsoleConstants.MediumPublicationCost, MediumResearchDiskPrototype),

            PublicationTier.Large =>
                (PeerReviewConsoleConstants.LargePublicationCost, LargeResearchDiskPrototype),

            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
        };

        if (ent.Comp.StoredValue < cost)
        {
            _popup.PopupEntity(
                Loc.GetString("peer-review-console-not-enough", ("cost", cost)),
                ent.Owner,
                user);

            return;
        }

        ent.Comp.StoredValue -= cost;

        Spawn(diskPrototype, Transform(ent.Owner).Coordinates);
        _audio.PlayPvs(ent.Comp.PublishSound, ent.Owner);
        UpdateUi(ent);

        _popup.PopupEntity(
            Loc.GetString("peer-review-console-published", ("value", ent.Comp.StoredValue)),
            ent.Owner,
            user);
    }

    private void UpdateUi(Entity<PeerReviewConsoleComponent> ent)
    {
        var state = new PeerReviewConsoleUiState(ent.Comp.StoredValue);

        _userInterface.SetUiState(
            ent.Owner,
            PeerReviewConsoleUiKey.Key,
            state);
    }

    private bool IsPowered(EntityUid uid)
    {
        return TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) &&
               powerReceiver.Powered;
    }

    private bool HasConsoleAccess(EntityUid user, EntityUid console)
    {
        return !TryComp<AccessReaderComponent>(console, out var reader) ||
               _accessReader.IsAllowed(user, console, reader);
    }
}
