namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised directed on a weapon when attempt a melee attack.
/// </summary>
[ByRefEvent]
// Shitmed Change - Added Weapon and WeaponComponent
// Goobstation - Grab: Added IsHeavyAttack
public record struct AttemptMeleeEvent(EntityUid User, EntityUid Weapon, MeleeWeaponComponent WeaponComponent, bool IsHeavyAttack, bool Cancelled = false, string? Message = null);
