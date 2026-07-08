using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>iss14: State for the round-end advertisement config admin EUI (<c>adconfig</c>).</summary>
[Serializable, NetSerializable]
public sealed class RoundEndAdConfigEuiState(bool canEdit, bool enabled, string message) : EuiStateBase
{
    public readonly bool CanEdit = canEdit;

    /// <summary>Whether the round-end advertisement broadcast is enabled.</summary>
    public readonly bool Enabled = enabled;

    /// <summary>The raw advertisement message (may contain real newlines and literal "\n" sequences).</summary>
    public readonly string Message = message;
}

// ---- Client -> server messages ----

[Serializable, NetSerializable]
public sealed class RoundEndAdSetEnabledMessage(bool value) : EuiMessageBase
{
    public readonly bool Value = value;
}

[Serializable, NetSerializable]
public sealed class RoundEndAdSetMessageMessage(string message) : EuiMessageBase
{
    public readonly string Message = message;
}
