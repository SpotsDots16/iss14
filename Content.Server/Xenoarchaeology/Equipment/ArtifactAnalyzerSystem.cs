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
    private const string Tier1PrintoutPrototype = "ArtifactResearchPrintoutTier1";
    private const string Tier2PrintoutPrototype = "ArtifactResearchPrintoutTier2";
    private const string Tier3PrintoutPrototype = "ArtifactResearchPrintoutTier3";

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

    private void OnExtractButtonPressed(Entity<AnalysisConsoleComponent> ent, ref AnalysisConsoleExtractButtonPressedMessage args)
    {
        if (!TryGetArtifactFromConsole(ent, out var artifact))
            return;

        if (!_research.TryGetClientServer(ent, out var server, out var serverComponent))
            return;

        var sumResearch = 0;
        var printoutsToSpawn = new List<string>();

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

            printoutsToSpawn.Add(GetPrintoutPrototype(node.Comp.Depth));
        }

        // 4-16-25: It's a sad day when a scientist makes negative 5k research
        if (sumResearch <= 0)
            return;

        _research.ModifyServerPoints(server.Value, sumResearch, serverComponent);

        var printoutCoordinates = Transform(ent.Owner).Coordinates;

        foreach (var prototype in printoutsToSpawn)
        {
            Spawn(prototype, printoutCoordinates);
        }

        _audio.PlayPvs(ent.Comp.ExtractSound, artifact.Value);
        _popup.PopupEntity(
            Loc.GetString("analyzer-artifact-extract-popup"),
            artifact.Value,
            PopupType.Large);
    }

    private static string GetPrintoutPrototype(int depth)
    {
        return depth switch
        {
            <= 0 => Tier1PrintoutPrototype,
            1 => Tier2PrintoutPrototype,
            _ => Tier3PrintoutPrototype,
        };
    }
}

