using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.VoiceChat;

/// <summary>
///     Sent client -> server, one per captured audio frame (~20-40ms of Opus-encoded mono voice).
///     Delivered unreliably: a dropped voice frame is better than a stalled, ever-growing voice stream.
/// </summary>
public sealed class MsgVoiceChunk : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.Unreliable;

    /// <summary>Opus-encoded audio for a single frame.</summary>
    public byte[] Data = [];

    /// <summary>True if this was captured with the whisper key (shorter range).</summary>
    public bool Whisper;

    /// <summary>Monotonic per-speaker frame counter, used by the receiver's jitter buffer to order/drop frames.</summary>
    public ushort Sequence;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Whisper = buffer.ReadBoolean();
        Sequence = buffer.ReadUInt16();
        var length = buffer.ReadVariableInt32();
        Data = buffer.ReadBytes(length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Whisper);
        buffer.Write(Sequence);
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
    }
}
