// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Power.Components;

namespace Content.Goobstation.Shared.Augments;

/// <summary>
/// Goobstation: Raised on an entity in a charger to find a battery to charge.
/// It gets raised on the entity itself then, if no battery was found, relayed to equipped items.
/// </summary>
/// <remarks>
/// iss14: upstream defined this in ChargerSystem and raised it from the charger's battery search.
/// iss14's newer ChargerSystem has no equivalent hook yet, so nothing raises this event currently;
/// it is vendored so <see cref="AugmentStationRechargeComponent"/> handling compiles.
/// </remarks>
[ByRefEvent]
public record struct FindBatteryEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public Entity<BatteryComponent>? FoundBattery;
}
