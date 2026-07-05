namespace Content.Server.Kitchen.Components;

/// <summary>
///     iss14: restored for the Shitmed port (upstream removed it in the kitchen spike rework).
///     Marks items that are sharp — used by GhettoSurgerySystem to grant improvised
///     surgery tool capabilities (knives and shards as makeshift scalpels/saws).
/// </summary>
[RegisterComponent]
public sealed partial class SharpComponent : Component
{
    [DataField("butcherDelayModifier")]
    public float ButcherDelayModifier = 1.0f;

    /// <summary>
    /// Shitmed: Whether this item had <c>SurgeryToolComponent</c> before sharp was added.
    /// </summary>
    [DataField]
    public bool HadSurgeryTool;

    /// <summary>
    /// Shitmed: Whether this item had <c>ScalpelComponent</c> before sharp was added.
    /// </summary>
    [DataField]
    public bool HadScalpel;

    /// <summary>
    /// Shitmed: Whether this item had <c>BoneSawComponent</c> before sharp was added.
    /// </summary>
    [DataField]
    public bool HadBoneSaw;
}
