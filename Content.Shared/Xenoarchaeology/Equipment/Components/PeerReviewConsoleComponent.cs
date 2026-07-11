namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// Stores artifact research data submitted for peer review.
[RegisterComponent]
public sealed partial class PeerReviewConsoleComponent : Component
{
    /// Total publication value currently stored in the console.
    [DataField]
    public int StoredValue;
}
