namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Stores the publication value of an artifact research printout.
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactResearchPrintoutComponent : Component
{
    /// <summary>
    /// How much research data this printout contributes when submitted.
    /// </summary>
    [DataField]
    public int Value = 1;

    /// <summary>
    /// The name of the artifact when this printout was produced.
    /// </summary>
    [DataField]
    public string ArtifactName = string.Empty;

    /// <summary>
    /// The visible identity of the person who extracted the research.
    /// </summary>
    [DataField]
    public string ResearcherName = string.Empty;

    /// <summary>
    /// The total research points awarded by the extraction.
    /// </summary>
    [DataField]
    public int TotalResearch;

    /// <summary>
    /// The artifact nodes represented by this printout.
    /// </summary>
    [DataField]
    public List<ArtifactResearchNodeData> Nodes = new();
}

/// <summary>
/// A snapshot of one artifact node at the time its research was extracted.
/// </summary>
[DataDefinition]
public sealed partial class ArtifactResearchNodeData
{
    [DataField]
    public string NodeId = string.Empty;

    [DataField]
    public int Depth;

    [DataField]
    public int ExtractedResearch;

    [DataField]
    public string EffectDescription = string.Empty;

    public ArtifactResearchNodeData()
    {
    }

    public ArtifactResearchNodeData(
        string nodeId,
        int depth,
        int extractedResearch,
        string effectDescription)
    {
        NodeId = nodeId;
        Depth = depth;
        ExtractedResearch = extractedResearch;
        EffectDescription = effectDescription;
    }
}
