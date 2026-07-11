using System.Collections.Generic;
using Content.Server.Research.Systems;
using Content.Server.Xenoarchaeology.Artifact;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Xenoarchaeology.Equipment;

/// <inheritdoc />
public sealed partial class ArtifactAnalyzerSystem : SharedArtifactAnalyzerSystem
{
    private const string ResearchPrintoutPrototype = "ArtifactResearchPrintout";

    // One publication data point represents approximately 10% of
    // 6,250 extracted research points.
    private const int ResearchPointsPerData = 6250;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenoArtifactSystem _xenoArtifact = default!;

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
        var printoutResearchValues = new List<int>();

        foreach (var node in _xenoArtifact.GetAllNodes(artifact.Value))
        {
            var research = _xenoArtifact.GetResearchValue(node);

            _xenoArtifact.SetConsumedResearchValue(
                node,
                node.Comp.ConsumedResearchValue + research);

            sumResearch += research;

            // Only create a printout if this node contributed new research
            // during this extraction.
            if (research <= 0)
                continue;

            printoutResearchValues.Add(research);
        }

        // 4-16-25: It's a sad day when a scientist makes negative 5k research
        if (sumResearch <= 0)
            return;

        _research.ModifyServerPoints(server.Value, sumResearch, serverComponent);

        var printoutCoordinates = Transform(ent.Owner).Coordinates;

        foreach (var research in printoutResearchValues)
        {
            var printout = Spawn(
                ResearchPrintoutPrototype,
                printoutCoordinates);

            // Integer division rounds down.
            // Examples:
            // 8,000 research = 1 data
            // 24,000 research = 3 data
            var printoutValue = research / ResearchPointsPerData;

            // Every node that contributes research should produce
            // a usable printout worth at least one data.
            if (printoutValue < 1)
                printoutValue = 1;

            if (TryComp<ArtifactResearchPrintoutComponent>(
                    printout,
                    out var printoutComponent))
            {
                printoutComponent.Value = printoutValue;
            }
        }

        _audio.PlayPvs(ent.Comp.ExtractSound, artifact.Value);
        _popup.PopupEntity(
            Loc.GetString("analyzer-artifact-extract-popup"),
            artifact.Value,
            PopupType.Large);
    }
}

