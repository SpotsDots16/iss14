namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// Stores the publication value of an artifact research printout.
[RegisterComponent]
public sealed partial class ArtifactResearchPrintoutComponent : Component
{
    /// How much research data this printout contributes when submitted.
    [DataField]
    public int Value = 1;
}