// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text.Json.Serialization;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adjusts bone damage on all of the body's woundables, spreading the amount evenly between them.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustBoneDamageEntityEffectSystem : EntityEffectSystem<BodyComponent, AdjustBoneDamage>
{
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private WoundSystem _wound = default!;

    protected override void Effect(Entity<BodyComponent> entity, ref EntityEffectEvent<AdjustBoneDamage> args)
    {
        if (entity.Comp.RootContainer.ContainedEntities.FirstOrNull() is not { } root)
            return;

        var woundables = _wound.GetAllWoundableChildren(root).ToList();
        foreach (var woundable in woundables)
        {
            if (woundable.Comp.Bone.ContainedEntities.FirstOrNull() is not { } bone)
                continue;

            // Yeah this is less efficient when theres not as many parts damaged but who tf cares,
            // its a bone medication so it should probs be strong enough to ignore this.
            _trauma.ApplyDamageToBone(bone, args.Effect.Amount / woundables.Count);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
[UsedImplicitly]
public sealed partial class AdjustBoneDamage : EntityEffectBase<AdjustBoneDamage>
{
    [DataField(required: true)]
    [JsonPropertyName("amount")]
    public FixedPoint2 Amount = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-bone-damage", ("amount", Amount));
}
