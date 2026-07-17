// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Trauma-Station (UnequipOldGear only; the antag-smite APIs were not ported).

using Content.Shared.Inventory;

namespace Content.Server.Antag;

/// <summary>
/// Trauma - various api additions
/// </summary>
public sealed partial class AntagSelectionSystem
{
    [Dependency] private InventorySystem _antagInventory = default!;

    public void UnequipOldGear(EntityUid player)
    {
        if (!TryComp<InventoryComponent>(player, out var comp))
            return;

        foreach (var slot in comp.Slots)
        {
            _antagInventory.TryUnequip(player, slot.Name, true, true, inventory: comp);
        }
    }
}
