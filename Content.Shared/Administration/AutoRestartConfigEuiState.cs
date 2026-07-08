using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>iss14: State for the scheduled auto-restart config admin EUI (<c>autorestartconfig</c>).</summary>
[Serializable, NetSerializable]
public sealed class AutoRestartConfigEuiState(
    bool canEdit,
    bool enabled,
    string time,
    long serverNowTicks,
    long nextRestartTicks,
    bool pending)
    : EuiStateBase
{
    public readonly bool CanEdit = canEdit;

    /// <summary>Whether the scheduled daily restart is enabled.</summary>
    public readonly bool Enabled = enabled;

    /// <summary>The configured restart time of day, 24h "HH:mm".</summary>
    public readonly string Time = time;

    /// <summary>Server local time when this state was built, as DateTime ticks.</summary>
    public readonly long ServerNowTicks = serverNowTicks;

    /// <summary>Next scheduled restart moment (server local time) as DateTime ticks, or 0 if none.</summary>
    public readonly long NextRestartTicks = nextRestartTicks;

    /// <summary>Whether the restart time has been reached and the server restarts at the end of the round.</summary>
    public readonly bool Pending = pending;
}

// ---- Client -> server messages ----

[Serializable, NetSerializable]
public sealed class AutoRestartSetEnabledMessage(bool value) : EuiMessageBase
{
    public readonly bool Value = value;
}

[Serializable, NetSerializable]
public sealed class AutoRestartSetTimeMessage(string time) : EuiMessageBase
{
    public readonly string Time = time;
}
