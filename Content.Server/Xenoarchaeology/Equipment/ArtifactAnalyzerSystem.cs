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

    // 10% of 6,250 research points = 1 publication data
    private const int ResearchPointsPerData = 6250;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenoArtifactSystem _xenoArtifact = default!;

    private sealed record PendingPrintout(
    string NodeId,
    int Depth,
    int ExtractedResearch,
    int DataValue);

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
        var printoutsToSpawn = new List<PendingPrintout>();

        foreach (var node in _xenoArtifact.GetAllNodes(artifact.Value))
        {

            var research = _xenoArtifact.GetResearchValue(node);
            var nodeId = _xenoArtifact.GetNodeId(node);

            _xenoArtifact.SetConsumedResearchValue(
                node,
                node.Comp.ConsumedResearchValue + research);

            sumResearch += research;

            // No new research from this node, so no printout.
            if (research <= 0)
                continue;

            // Integer division rounds down.
            var printoutValue = Math.Max(1, research / ResearchPointsPerData);

            printoutsToSpawn.Add(new PendingPrintout(
    nodeId,
    node.Comp.Depth,
    research,
    printoutValue));
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

        Timer.Spawn(ent.Comp.PrintoutDelay, () =>
        {
            if (Deleted(analyzer))
                return;

            var printoutCoordinates = Transform(analyzer).Coordinates;

            foreach (var printoutData in printoutsToSpawn)
            {
                var printout = Spawn(
                    ResearchPrintoutPrototype,
                    printoutCoordinates);

                if (TryComp<ArtifactResearchPrintoutComponent>(
                        printout,
                        out var printoutComponent))
                {
                    printoutComponent.Value = printoutData.DataValue;
                }

                WritePrintout(printout, artifactName, printoutData);
            }

            _audio.PlayPvs(ent.Comp.PrintoutSound, analyzer);
        });
    }

    private void WritePrintout(
    EntityUid printout,
    string artifactName,
    PendingPrintout data)
    {
        var msg = new FormattedMessage();

        msg.AddText("SCINET XENOARCHAEOLOGICAL ANALYSIS REPORT\n");
        msg.AddText("========================================\n\n");
        msg.AddText($"Artifact subject: {artifactName}\n");
        msg.AddText($"Node ID: {data.NodeId}\n");
        msg.AddText($"Node depth: {data.Depth}\n");
        msg.AddText($"Research extracted: {data.ExtractedResearch:N0} points\n");
        msg.AddText($"Publication data value: {data.DataValue}\n\n");
        msg.AddText("Status: Awaiting peer review\n");
        msg.AddText("Origin: Artifact analyzer extraction\n");

        _paper.SetContent(printout, msg.ToMarkup());
    }
}

