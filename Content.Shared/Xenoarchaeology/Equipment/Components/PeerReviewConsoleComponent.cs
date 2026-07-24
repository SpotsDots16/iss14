using Robust.Shared.Audio;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Stores artifact research data submitted for peer review.
/// </summary>
[RegisterComponent]
public sealed partial class PeerReviewConsoleComponent : Component
{
    /// <summary>
    /// Submitted printouts in insertion order. Publication data is consumed from the oldest entry first.
    /// </summary>
    [DataField]
    public List<StoredArtifactSubmission> Submissions = new();

    /// <summary>
    /// Total publication value currently stored in the console.
    /// </summary>
    public int StoredValue
    {
        get
        {
            var total = 0;

            foreach (var submission in Submissions)
            {
                total += submission.RemainingValue;
            }

            return total;
        }
    }

    /// <summary>
    /// Sound played when a research printout is inserted.
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound;

    /// <summary>
    /// Sound played when a research disk is published.
    /// </summary>
    [DataField]
    public SoundSpecifier? PublishSound;

    /// <summary>
    /// Sound played when the console interface is opened.
    /// </summary>
    [DataField]
    public SoundSpecifier? KeyboardSound;
}

/// <summary>
/// The research and attribution metadata retained from a submitted analyzer printout.
/// </summary>
[DataDefinition]
public sealed partial class StoredArtifactSubmission
{
    [DataField]
    public int RemainingValue;

    [DataField]
    public string ArtifactName = string.Empty;

    [DataField]
    public string ResearcherName = string.Empty;

    [DataField]
    public int TotalResearch;

    [DataField]
    public List<ArtifactResearchNodeData> Nodes = new();

    public StoredArtifactSubmission()
    {
    }

    public StoredArtifactSubmission(
        int remainingValue,
        string artifactName,
        string researcherName,
        int totalResearch,
        List<ArtifactResearchNodeData> nodes)
    {
        RemainingValue = remainingValue;
        ArtifactName = artifactName;
        ResearcherName = researcherName;
        TotalResearch = totalResearch;
        Nodes = nodes;
    }
}
