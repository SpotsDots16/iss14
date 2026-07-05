using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// State for the entity census admin EUI (<c>entitycensus</c> command).
/// The most numerous entity prototypes on the server and how fast they are growing.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityCensusEuiState(
    List<EntityCensusEntry> entries,
    int totalEntities,
    bool minuteAvailable) : EuiStateBase
{
    /// <summary>Top prototypes by live entity count, largest first.</summary>
    public readonly List<EntityCensusEntry> Entries = entries;

    /// <summary>All entities on the server, including ones not in <see cref="Entries"/>.</summary>
    public readonly int TotalEntities = totalEntities;

    /// <summary>False while less than a minute of census history exists (minute deltas are then 0).</summary>
    public readonly bool MinuteAvailable = minuteAvailable;
}

[Serializable, NetSerializable]
public struct EntityCensusEntry(string prototype, int count, int roundDelta, int minuteDelta)
{
    /// <summary>Entity prototype ID; the client resolves the display name locally.</summary>
    public string Prototype = prototype;

    public int Count = count;

    /// <summary>Change compared to the census taken at round start.</summary>
    public int RoundDelta = roundDelta;

    /// <summary>Change compared to roughly one minute ago.</summary>
    public int MinuteDelta = minuteDelta;
}
