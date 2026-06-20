using Robust.Shared.Serialization;

namespace Content.Shared.VoiceChat;

/// <summary>
///     Raised by a client (networked to the server) when the local player enables or disables their proximity
///     voice chat, so the server can add/remove <see cref="VoiceMutedComponent"/> on their entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMuteStateChangedEvent : EntityEventArgs
{
    public bool Muted;

    public VoiceMuteStateChangedEvent(bool muted)
    {
        Muted = muted;
    }
}
