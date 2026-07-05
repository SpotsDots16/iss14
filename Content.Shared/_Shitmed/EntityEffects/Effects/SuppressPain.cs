using System.Text.Json.Serialization;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Pain.Systems;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Suppresses the target's pain by adding a negative pain modifier to their head, scaled by the effect strength.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SuppressPainEntityEffectSystem : EntityEffectSystem<ConsciousnessComponent, SuppressPain>
{
    [Dependency] private ConsciousnessSystem _consciousness = default!;
    [Dependency] private PainSystem _pain = default!;
    [Dependency] private SharedBodySystem _body = default!;

    protected override void Effect(Entity<ConsciousnessComponent> entity, ref EntityEffectEvent<SuppressPain> args)
    {
        if (!_consciousness.TryGetNerveSystem(entity, out var nerveSys))
            return;

        var bodyPart = _body.GetBodyChildrenOfType(entity, BodyPartType.Head)
            .FirstOrNull();

        if (bodyPart == null)
            return;

        var change = args.Effect.Amount * args.Scale;

        if (!_pain.TryGetPainModifier(nerveSys.Value, bodyPart.Value.Id, args.Effect.ModifierIdentifier, out var modifier))
        {
            _pain.TryAddPainModifier(nerveSys.Value,
                bodyPart.Value.Id,
                args.Effect.ModifierIdentifier,
                -change,
                time: args.Effect.Time);
        }
        else
        {
            _pain.TryChangePainModifier(nerveSys.Value,
                bodyPart.Value.Id,
                args.Effect.ModifierIdentifier,
                modifier.Value.Change - change,
                time: args.Effect.Time);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
[UsedImplicitly]
public sealed partial class SuppressPain : EntityEffectBase<SuppressPain>
{
    [DataField(required: true)]
    [JsonPropertyName("amount")]
    public FixedPoint2 Amount = default!;

    [DataField(required: true)]
    [JsonPropertyName("time")]
    public TimeSpan Time = default!;

    [DataField]
    [JsonPropertyName("identifier")]
    public string ModifierIdentifier = "PainSuppressant";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-suppress-pain", ("chance", Probability));
}
