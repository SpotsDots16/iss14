// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Trauma-Station.

using Content.Server._Trauma.GameTicking.Rules;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._Trauma.GameTicking.Rules.Components;

/// <summary>
/// Starts every nested gamerule an entity table picks.
/// </summary>
[RegisterComponent, Access(typeof(NestedRuleSystem))]
public sealed partial class NestedRuleComponent : Component
{
    /// <summary>
    /// The gamerules to start.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Rules = default!;
}
