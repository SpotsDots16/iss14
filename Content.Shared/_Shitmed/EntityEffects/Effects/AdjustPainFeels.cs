using System.Text.Json.Serialization;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Pain.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Randomly nudges the pain feels modifier on each of the target's body parts, scaled by the effect strength.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustPainFeelsEntityEffectSystem : EntityEffectSystem<ConsciousnessComponent, AdjustPainFeels>
{
    [Dependency] private ConsciousnessSystem _consciousness = default!;
    [Dependency] private PainSystem _pain = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(Entity<ConsciousnessComponent> entity, ref EntityEffectEvent<AdjustPainFeels> args)
    {
        if (!_consciousness.TryGetNerveSystem(entity, out var nerveSys))
            return;

        var amount = args.Effect.Amount;
        var scale = args.Scale;

        foreach (var bodyPart in _body.GetBodyChildren(entity))
        {
            if (!_pain.TryGetPainFeelsModifier(bodyPart.Id, nerveSys.Value, args.Effect.ModifierIdentifier, out _))
            {
                _pain.TryAddPainFeelsModifier(
                    nerveSys.Value,
                    args.Effect.ModifierIdentifier,
                    bodyPart.Id,
                    _random.Prob(0.3f) ? amount * scale : -amount * scale);
            }
            else
            {
                var add = _random.Prob(0.3f) ? amount : -amount;
                _pain.TryChangePainFeelsModifier(
                    nerveSys.Value,
                    args.Effect.ModifierIdentifier,
                    bodyPart.Id,
                    add * scale);
            }
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
[UsedImplicitly]
public sealed partial class AdjustPainFeels : EntityEffectBase<AdjustPainFeels>
{
    [DataField(required: true)]
    [JsonPropertyName("amount")]
    public FixedPoint2 Amount = default!;

    [DataField]
    [JsonPropertyName("identifier")]
    public string ModifierIdentifier = "PainSuppressant";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-suppress-pain", ("chance", Probability));
}
