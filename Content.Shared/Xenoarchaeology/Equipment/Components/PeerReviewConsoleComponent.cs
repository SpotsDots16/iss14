using Robust.Shared.Audio;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// Stores artifact research data submitted for peer review.
[RegisterComponent]
public sealed partial class PeerReviewConsoleComponent : Component
{
    /// Total publication value currently stored in the console.
    [DataField]
    public int StoredValue;

    /// Sound played when a research printout is inserted.
    [DataField]
    public SoundSpecifier InsertSound = default!;

    /// Sound played when a research disk is published.
    [DataField]
    public SoundSpecifier PublishSound = default!;

    /// Sound played when the console interface is opened.
    [DataField]
    public SoundSpecifier KeyboardSound = default!;

}

