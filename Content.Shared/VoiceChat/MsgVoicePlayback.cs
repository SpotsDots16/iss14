using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.VoiceChat;

/// <summary>
///     Sent server -> client, relaying one speaker's voice frame to a listener that is in range.
///     The server only sends these to clients whose attached entity is within the speaker's voice range.
/// </summary>
public sealed class MsgVoicePlayback : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.Unreliable;

    /// <summary>The entity that is speaking. Used to position the audio source and key the jitter buffer.</summary>
    public NetEntity Speaker;

    /// <summary>Opus-encoded audio for a single frame.</summary>
    public byte[] Data = [];

    /// <summary>True if this was whispered (shorter range / quieter playback).</summary>
    public bool Whisper;

    /// <summary>Per-speaker frame counter from the originating <see cref="MsgVoiceChunk"/>.</summary>
    public ushort Sequence;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Speaker = buffer.ReadNetEntity();
        Whisper = buffer.ReadBoolean();
        Sequence = buffer.ReadUInt16();
        var length = buffer.ReadVariableInt32();
        Data = buffer.ReadBytes(length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Speaker);
        buffer.Write(Whisper);
        buffer.Write(Sequence);
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
    }
}
