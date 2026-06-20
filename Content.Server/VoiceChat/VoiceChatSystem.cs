using Content.Shared.CCVar;
using Content.Shared.VoiceChat;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.VoiceChat;

/// <summary>
///     Server side of proximity voice chat. Receives Opus voice frames from a speaking client and relays
///     them, unmodified, to every player whose entity is within the speaker's voice range. The server never
///     decodes the audio for relaying (it just forwards opaque Opus bytes); decoding only happens for the
///     optional transcription pass.
/// </summary>
public sealed partial class VoiceChatSystem : EntitySystem
{
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<MsgVoiceChunk>(OnVoiceChunk);
        _net.RegisterNetMessage<MsgVoicePlayback>();

        SubscribeNetworkEvent<VoiceMuteStateChangedEvent>(OnMuteStateChanged);
    }

    private void OnMuteStateChanged(VoiceMuteStateChangedEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (ev.Muted)
            EnsureComp<VoiceMutedComponent>(uid);
        else
            RemComp<VoiceMutedComponent>(uid);
    }

    private void OnVoiceChunk(MsgVoiceChunk message)
    {
        // Voice can be disabled for the whole round by an admin.
        if (!_cfg.GetCVar(CCVars.VoiceEnabled))
            return;

        if (!_player.TryGetSessionByChannel(message.MsgChannel, out var session))
            return;

        // A speaker without a body has no position to attach proximity audio to.
        if (session.AttachedEntity is not { } speaker)
            return;

        var range = message.Whisper
            ? _cfg.GetCVar(CCVars.VoiceWhisperRange)
            : _cfg.GetCVar(CCVars.VoiceRange);

        if (range <= 0f)
            return;

        var origin = _transform.GetMapCoordinates(speaker);

        var relay = new MsgVoicePlayback
        {
            Speaker = GetNetEntity(speaker),
            Whisper = message.Whisper,
            Sequence = message.Sequence,
            Data = message.Data,
        };

        foreach (var recipient in Filter.Empty().AddInRange(origin, range).Recipients)
        {
            // Don't echo a speaker's own voice back to them.
            if (recipient == session)
                continue;

            _net.ServerSendMessage(relay, recipient.Channel);
        }

        // Transcription (optional) hooks in here in a later layer: decode message.Data and log it.
    }
}
