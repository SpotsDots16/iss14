using Content.Shared.Atmos;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Adjust the amount of Moles stored in this set of lungs based on a given dictionary of gasses and ratios.
/// The amount of gas adjusted is modified by scale.
/// iss14: the effect system lives in Content.Server since LungComponent is server-only in the restored body system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyLungGas : EntityEffectBase<ModifyLungGas>
{
    [DataField(required: true)]
    public Dictionary<Gas, float> Ratios = default!;
}
