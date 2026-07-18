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
}
