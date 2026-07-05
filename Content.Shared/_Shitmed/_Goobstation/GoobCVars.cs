using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Goobstation.Common.CCVar;

/// <summary>
/// Minimal vendored subset of Goob-Station's GoobCVars needed by the Shitmed port.
/// </summary>
[CVarDefs]
public sealed partial class GoobCVars : CVars
{
    /// <summary>
    /// A multiplier for bloodloss damage and heal.
    /// </summary>
    public static readonly CVarDef<float> BleedMultiplier =
        CVarDef.Create("medical.bloodloss_multiplier", 4.0f, CVar.SERVER);
}
