using System.Text.Json.Serialization;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds (or edits) a consciousness modifier on the target's nerve system, scaled by the effect strength.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustConsciousnessEntityEffectSystem : EntityEffectSystem<ConsciousnessComponent, AdjustConsciousness>
{
    [Dependency] private ConsciousnessSystem _consciousness = default!;

    protected override void Effect(Entity<ConsciousnessComponent> entity, ref EntityEffectEvent<AdjustConsciousness> args)
    {
        if (!_consciousness.TryGetNerveSystem(entity, out var nerveSys))
            return;

        var amount = args.Effect.Amount * args.Scale;

        if (args.Effect.AllowNewModifiers)
        {
            if (!_consciousness.EditConsciousnessModifier(entity,
                    nerveSys.Value.Owner,
                    amount,
                    args.Effect.Identifier,
                    args.Effect.Time))
            {
                _consciousness.AddConsciousnessModifier(entity,
                    nerveSys.Value.Owner,
                    amount,
                    args.Effect.Identifier,
                    args.Effect.ModifierType,
                    args.Effect.Time);
            }
        }
        else
        {
            _consciousness.EditConsciousnessModifier(entity,
                nerveSys.Value.Owner,
                amount,
                args.Effect.Identifier,
                args.Effect.Time);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
[UsedImplicitly]
public sealed partial class AdjustConsciousness : EntityEffectBase<AdjustConsciousness>
{
    [DataField(required: true)]
    [JsonPropertyName("amount")]
    public FixedPoint2 Amount = default!;

    [DataField(required: true)]
    [JsonPropertyName("time")]
    public TimeSpan Time = default!;

    [DataField]
    [JsonPropertyName("identifier")]
    public string Identifier = "ConsciousnessModifier";

    [DataField]
    [JsonPropertyName("allowNewModifiers")]
    public bool AllowNewModifiers = true;

    [DataField]
    [JsonPropertyName("modifierType")]
    public ConsciousnessModType ModifierType = ConsciousnessModType.Generic;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-consciousness");
}
