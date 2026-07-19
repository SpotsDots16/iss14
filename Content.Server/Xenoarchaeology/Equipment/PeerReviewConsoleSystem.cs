using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Equipment;

public sealed partial class PeerReviewConsoleSystem : EntitySystem
{
    private const string SmallResearchDiskPrototype = "ResearchDisk";
    private const string MediumResearchDiskPrototype = "ResearchDisk5000";
    private const string LargeResearchDiskPrototype = "ResearchDisk10000";
    private const string PublicationReportPrototype = "ArtifactPublicationReport";

    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paper = default!;

    internal sealed record PublicationContribution(StoredArtifactSubmission Submission, int DataUsed);

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

        ent.Comp.Submissions.Add(new StoredArtifactSubmission(
            printout.Value,
            printout.ArtifactName,
            printout.ResearcherName,
            printout.TotalResearch,
            CopyNodes(printout.Nodes)));

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

        if (!TryConsumeData(ent.Comp, cost, out var contributions))
            return;

        Spawn(diskPrototype, Transform(ent.Owner).Coordinates);

        var report = Spawn(PublicationReportPrototype, Transform(ent.Owner).Coordinates);
        var tierName = GetTierName(tier);
        var publisherName = Identity.Name(user, EntityManager);

        _metaData.SetEntityName(
            report,
            Loc.GetString("peer-review-publication-paper-name", ("tier", tierName)));

        _metaData.SetEntityDescription(
            report,
            Loc.GetString("peer-review-publication-paper-description", ("tier", tierName)));

        WritePublicationReport(report, tier, tierName, cost, publisherName, contributions);

        _audio.PlayPvs(ent.Comp.PublishSound, ent.Owner);
        UpdateUi(ent);

        _popup.PopupEntity(
            Loc.GetString("peer-review-console-published", ("value", ent.Comp.StoredValue)),
            ent.Owner,
            user);
    }

    /// <summary>
    /// Consumes publication data from the oldest submitted printouts first.
    /// A printout may be partially consumed, leaving its remaining value and attribution in the queue.
    /// </summary>
    internal static bool TryConsumeData(
        PeerReviewConsoleComponent component,
        int cost,
        out List<PublicationContribution> contributions)
    {
        contributions = new List<PublicationContribution>();

        if (cost <= 0 || component.StoredValue < cost)
            return false;

        var remainingCost = cost;

        while (remainingCost > 0)
        {
            var submission = component.Submissions[0];

            if (submission.RemainingValue <= 0)
            {
                component.Submissions.RemoveAt(0);
                continue;
            }

            var usedValue = Math.Min(submission.RemainingValue, remainingCost);

            contributions.Add(new PublicationContribution(submission, usedValue));

            submission.RemainingValue -= usedValue;
            remainingCost -= usedValue;

            if (submission.RemainingValue == 0)
                component.Submissions.RemoveAt(0);
        }

        return true;
    }

    private void WritePublicationReport(
        EntityUid report,
        PublicationTier tier,
        string tierName,
        int dataUsed,
        string publisherName,
        List<PublicationContribution> contributions)
    {
        var msg = new FormattedMessage();
        var researcherNames = contributions
            .Select(contribution => contribution.Submission.ResearcherName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var researchers = researcherNames.Count > 0
            ? string.Join(", ", researcherNames)
            : Loc.GetString("peer-review-publication-unknown-researcher");

        msg.AddText(Loc.GetString("peer-review-publication-report-header") + "\n");
        msg.AddText("========================================\n\n");
        msg.AddText(Loc.GetString("peer-review-publication-report-tier", ("tier", tierName)) + "\n");
        msg.AddText(Loc.GetString(
            "peer-review-publication-report-source-count",
            ("count", contributions.Count)) + "\n");
        msg.AddText(Loc.GetString("peer-review-publication-report-data-used", ("value", dataUsed)) + "\n");
        msg.AddText(Loc.GetString("peer-review-publication-report-researchers", ("names", researchers)) + "\n");
        msg.AddText(Loc.GetString("peer-review-publication-report-publisher", ("name", publisherName)) + "\n\n");
        msg.AddText(Loc.GetString("peer-review-publication-report-findings-header") + "\n");
        msg.AddText("----------------------------------------\n");

        for (var i = 0; i < contributions.Count; i++)
        {
            var contribution = contributions[i];
            var submission = contribution.Submission;
            var artifactName = string.IsNullOrWhiteSpace(submission.ArtifactName)
                ? Loc.GetString("peer-review-publication-unknown-artifact")
                : submission.ArtifactName;
            var researcherName = string.IsNullOrWhiteSpace(submission.ResearcherName)
                ? Loc.GetString("peer-review-publication-unknown-researcher")
                : submission.ResearcherName;

            msg.AddText("\n" + Loc.GetString(
                "peer-review-publication-report-source-header",
                ("number", i + 1),
                ("artifact", artifactName)) + "\n");
            msg.AddText(Loc.GetString(
                "peer-review-publication-report-source-researcher",
                ("name", researcherName)) + "\n");
            msg.AddText(Loc.GetString(
                "peer-review-publication-report-source-data",
                ("value", contribution.DataUsed)) + "\n");
            msg.AddText(Loc.GetString(
                "peer-review-publication-report-source-research",
                ("points", submission.TotalResearch)) + "\n");

            if (submission.Nodes.Count == 0)
            {
                msg.AddText(Loc.GetString("peer-review-publication-report-no-node-data") + "\n");
                continue;
            }

            foreach (var node in submission.Nodes)
            {
                var effect = string.IsNullOrWhiteSpace(node.EffectDescription)
                    ? Loc.GetString("peer-review-publication-unknown-effect")
                    : node.EffectDescription;

                msg.AddText(Loc.GetString(
                    "peer-review-publication-report-node-line",
                    ("id", node.NodeId),
                    ("depth", node.Depth),
                    ("effect", effect),
                    ("points", node.ExtractedResearch)) + "\n");
            }
        }

        msg.AddText("\n" + Loc.GetString("peer-review-publication-report-conclusion-header") + "\n");
        msg.AddText(GetTierConclusion(tier) + "\n\n");
        msg.AddText(Loc.GetString("peer-review-publication-report-status") + "\n");
        msg.AddText(Loc.GetString("peer-review-publication-report-origin") + "\n");

        _paper.SetContent(report, msg.ToMarkup());
    }

    private string GetTierName(PublicationTier tier)
    {
        return tier switch
        {
            PublicationTier.Small => Loc.GetString("peer-review-publication-tier-minor"),
            PublicationTier.Medium => Loc.GetString("peer-review-publication-tier-significant"),
            PublicationTier.Large => Loc.GetString("peer-review-publication-tier-major"),
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
        };
    }

    private string GetTierConclusion(PublicationTier tier)
    {
        return tier switch
        {
            PublicationTier.Small => Loc.GetString("peer-review-publication-conclusion-minor"),
            PublicationTier.Medium => Loc.GetString("peer-review-publication-conclusion-significant"),
            PublicationTier.Large => Loc.GetString("peer-review-publication-conclusion-major"),
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
        };
    }

    private static List<ArtifactResearchNodeData> CopyNodes(List<ArtifactResearchNodeData> nodes)
    {
        var result = new List<ArtifactResearchNodeData>(nodes.Count);

        foreach (var node in nodes)
        {
            result.Add(new ArtifactResearchNodeData(
                node.NodeId,
                node.Depth,
                node.ExtractedResearch,
                node.EffectDescription));
        }

        return result;
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
