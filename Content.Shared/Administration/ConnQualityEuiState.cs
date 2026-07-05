using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// State for the connection quality admin EUI (<c>connquality</c> command).
/// One row per connected player, refreshed once per second while open.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConnQualityEuiState(List<ConnQualityEntry> entries) : EuiStateBase
{
    public readonly List<ConnQualityEntry> Entries = entries;
}

[Serializable, NetSerializable]
public struct ConnQualityEntry(
    string oocName,
    string characterName,
    short pingMs,
    string status,
    TimeSpan connectedFor)
{
    public string OocName = oocName;
    public string CharacterName = characterName;

    /// <summary>Average round-trip time to the player in milliseconds.</summary>
    public short PingMs = pingMs;

    public string Status = status;
    public TimeSpan ConnectedFor = connectedFor;
}
