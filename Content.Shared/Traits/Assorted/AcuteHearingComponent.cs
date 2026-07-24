namespace Content.Shared.Traits.Assorted;

/// <summary>
/// iss14: gives an entity better-than-normal hearing. Speech and whispers can be heard from
/// farther away, and "psps" cat calls carry even farther. Used by cat species (Tajaran).
/// Counterpart to <see cref="DeafComponent"/> / <see cref="HardOfHearingComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class AcuteHearingComponent : Component
{
    /// <summary>
    /// Multiplier applied to how far this entity can hear speech and whispers.
    /// 1.2 = hears everything 20% farther than a normal listener.
    /// </summary>
    [DataField]
    public float RangeMultiplier = 1.2f;

    /// <summary>
    /// Multiplier used instead of <see cref="RangeMultiplier"/> when the message is a
    /// "psps" call, which carries much farther for these ears.
    /// </summary>
    [DataField]
    public float PspsRangeMultiplier = 2f;
}
