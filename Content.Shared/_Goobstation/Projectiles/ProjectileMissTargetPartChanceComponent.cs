// Goobstation - ranged aim-miss falloff (adapted from Goob-Station)
using Robust.Shared.Map;

namespace Content.Goobstation.Shared.Projectiles;

/// <summary>
/// Stamped on projectiles fired by a shooter with a targeting doll. On hit, the aimed body part
/// only applies with a chance that falls off with the distance the shot travelled - beyond
/// ~2 tiles the hit degrades to the chest.
/// </summary>
/// <remarks>
/// iss14: Goob rolls per-body at fire time into a PerfectHitEntities list; we store the fire
/// position and roll at hit time instead, which distills to the same shot-distance falloff
/// without an entity lookup per shot.
/// </remarks>
[RegisterComponent]
public sealed partial class ProjectileMissTargetPartChanceComponent : Component
{
    /// <summary>
    /// Where the shot was fired from.
    /// </summary>
    [ViewVariables]
    public MapCoordinates? FireCoordinates;
}
