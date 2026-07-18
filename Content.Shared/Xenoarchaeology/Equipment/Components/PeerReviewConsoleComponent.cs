using Robust.Shared.Audio;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Stores artifact research data submitted for peer review.
/// </summary>
[RegisterComponent]
public sealed partial class PeerReviewConsoleComponent : Component
{
    /// <summary>
    /// Total publication value currently stored in the console.
    /// </summary>
    [DataField]
    public int StoredValue;

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
