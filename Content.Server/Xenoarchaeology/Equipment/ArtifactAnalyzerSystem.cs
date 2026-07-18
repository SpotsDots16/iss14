using System;
using System.Collections.Generic;
using Content.Shared.Paper;
using Content.Server.Research.Systems;
using Content.Server.Xenoarchaeology.Artifact;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.Equipment;

/// <inheritdoc />
public sealed partial class ArtifactAnalyzerSystem : SharedArtifactAnalyzerSystem
{
    private const string ResearchPrintoutPrototype = "ArtifactResearchPrintout";

    /// <summary>
    /// How many extracted research points make up one unit of publication data on a printout.
    /// </summary>
    private const int ResearchPointsPerData = 6250;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenoArtifactSystem _xenoArtifact = default!;

    private sealed record PrintoutNodeLine(string NodeId, int Depth, int ExtractedResearch);

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleExtractButtonPressedMessage>(OnExtractButtonPressed);
    }

    private void OnExtractButtonPressed(
        Entity<AnalysisConsoleComponent> ent,
        ref AnalysisConsoleExtractButtonPressedMessage args)
    {
        if (!TryGetArtifactFromConsole(ent, out var artifact))
            return;

        if (!_research.TryGetClientServer(ent, out var server, out var serverComponent))
            return;

        var sumResearch = 0;
        var nodeLines = new List<PrintoutNodeLine>();

        foreach (var node in _xenoArtifact.GetAllNodes(artifact.Value))
        {
            var research = _xenoArtifact.GetResearchValue(node);

            _xenoArtifact.SetConsumedResearchValue(
                node,
                node.Comp.ConsumedResearchValue + research);

            sumResearch += research;

            // No new research from this node, so it doesn't appear on the report.
            if (research <= 0)
                continue;

            nodeLines.Add(new PrintoutNodeLine(_xenoArtifact.GetNodeId(node), node.Comp.Depth, research));
        }

        // No new research extracted.
        if (sumResearch <= 0)
            return;

        _research.ModifyServerPoints(server.Value, sumResearch, serverComponent);

        var analyzer = ent.Owner;
        var artifactName = Name(artifact.Value);

        _audio.PlayPvs(ent.Comp.ExtractSound, artifact.Value);
        _popup.PopupEntity(
            Loc.GetString("analyzer-artifact-extract-popup"),
            artifact.Value,
            PopupType.Large);

        // Publication data comes from the whole extraction, not per node: this keeps the
        // points-per-data rate honest for artifacts with many small nodes. Too little research
        // means no publishable printout at all.
        var dataValue = sumResearch / ResearchPointsPerData;
        if (dataValue <= 0)
            return;

        Timer.Spawn(ent.Comp.PrintoutDelay, () =>
        {
            if (Deleted(analyzer))
                return;

            var printout = Spawn(ResearchPrintoutPrototype, Transform(analyzer).Coordinates);

            if (TryComp<ArtifactResearchPrintoutComponent>(printout, out var printoutComponent))
                printoutComponent.Value = dataValue;

            WritePrintout(printout, artifactName, sumResearch, dataValue, nodeLines);

            _audio.PlayPvs(ent.Comp.PrintoutSound, analyzer);
        });
    }

    private void WritePrintout(
        EntityUid printout,
        string artifactName,
        int totalResearch,
        int dataValue,
        List<PrintoutNodeLine> nodeLines)
    {
        var msg = new FormattedMessage();

        msg.AddText(Loc.GetString("artifact-report-header") + "\n");
        msg.AddText("========================================\n\n");
        msg.AddText(Loc.GetString("artifact-report-subject", ("name", artifactName)) + "\n\n");

        foreach (var line in nodeLines)
        {
            msg.AddText(Loc.GetString("artifact-report-node-line",
                ("id", line.NodeId),
                ("depth", line.Depth),
                ("points", line.ExtractedResearch)) + "\n");
        }

        msg.AddText("\n");
        msg.AddText(Loc.GetString("artifact-report-total", ("points", totalResearch)) + "\n");
        msg.AddText(Loc.GetString("artifact-report-value", ("value", dataValue)) + "\n\n");
        msg.AddText(Loc.GetString("artifact-report-status") + "\n");
        msg.AddText(Loc.GetString("artifact-report-origin") + "\n");

        _paper.SetContent(printout, msg.ToMarkup());
    }
}
