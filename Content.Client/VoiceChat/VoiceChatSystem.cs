using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.VoiceChat;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.VoiceChat;

/// <summary>
///     Client side of proximity voice chat.
///     <para>
///     Send path: while a push-to-talk (or push-to-whisper) key is held, captures the microphone, encodes each
///     20 ms frame with Opus and streams it to the server as <see cref="MsgVoiceChunk"/>.
///     </para>
///     <para>
///     Receive path: decodes <see cref="MsgVoicePlayback"/> frames per speaker, feeding them into a positional
///     streaming audio source so other players are heard from their location with distance falloff.
///     </para>
/// </summary>
public sealed partial class VoiceChatSystem : EntitySystem
{
    [Dependency] private IAudioManager _audio = default!;
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    // On-screen "you are talking" speaker icon, built lazily the first time the player talks.
    private TextureRect? _talkIndicator;

    /// <summary>Number of OpenAL buffers per speaker; each holds one 20 ms frame, so this is the jitter cushion.</summary>
    private const int VoiceBufferCount = 8;

    // --- Send path ---
    private IOpusEncoder? _encoder;
    private IAudioInputDevice? _mic;
    private bool _talking;
    private bool _whispering;
    private ushort _sequence;

    private readonly short[] _frame = new short[VoiceChatConstants.FrameSamples];
    private readonly byte[] _encoded = new byte[VoiceChatConstants.MaxEncodedFrameBytes];

    // --- Receive path ---
    private readonly Dictionary<EntityUid, VoiceStream> _streams = new();
    private readonly List<EntityUid> _toRemove = new();

    private readonly short[] _decodeBuf = new short[VoiceChatConstants.FrameSamples];
    // The engine's mono WriteBuffer only uploads data.Length/2 samples, so a frame must be passed as a 2x-length span.
    private readonly ushort[] _uploadBuf = new ushort[VoiceChatConstants.FrameSamples * 2];

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<MsgVoiceChunk>();
        _net.RegisterNetMessage<MsgVoicePlayback>(OnVoicePlayback);

        // Push-to-talk / push-to-whisper are hold keys: capture starts on key-down, stops on key-up.
        // handle:false so the key press still passes through to anything else bound to it.
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.PushToTalk,
                InputCmdHandler.FromDelegate(_ => StartTalking(false), _ => StopTalking(), handle: false))
            .Bind(ContentKeyFunctions.PushToWhisper,
                InputCmdHandler.FromDelegate(_ => StartTalking(true), _ => StopTalking(), handle: false))
            .Register<VoiceChatSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<VoiceChatSystem>();
        CloseMic();
        _encoder = null;

        foreach (var stream in _streams.Values)
            stream.Dispose();
        _streams.Clear();

        _talkIndicator?.Dispose();
        _talkIndicator = null;
    }

    // ------------------------------------------------------------------
    // Send path
    // ------------------------------------------------------------------

    private void StartTalking(bool whisper)
    {
        // The server can disable voice for the whole round; the player can also mute their own mic locally.
        if (!_cfg.GetCVar(CCVars.VoiceEnabled) || !_cfg.GetCVar(CCVars.VoiceInputEnabled))
            return;

        // Allow switching talk<->whisper without releasing, but only (re)open the mic once.
        _whispering = whisper;

        if (_talking)
            return;

        _encoder ??= _audio.CreateOpusEncoder(VoiceChatConstants.SampleRate, VoiceChatConstants.Channels);

        if (_mic == null)
        {
            var device = _cfg.GetCVar(CCVars.VoiceInputDevice);
            _mic = _audio.OpenAudioInput(device, VoiceChatConstants.SampleRate);

            if (_mic == null)
            {
                Log.Warning("Voice: could not open a microphone; push-to-talk will do nothing.");
                return;
            }
        }

        _talking = true;
    }

    private void StopTalking()
    {
        if (!_talking)
            return;

        _talking = false;
        // Release the capture device so the mic is only live while a key is held (privacy + frees the device).
        CloseMic();
    }

    private void CloseMic()
    {
        _mic?.Dispose();
        _mic = null;
    }

    /// <summary>Scales the just-captured frame in place by the client's microphone-volume setting.</summary>
    private void ApplyInputGain()
    {
        var gain = _cfg.GetCVar(CCVars.VoiceInputVolume);
        if (Math.Abs(gain - 1f) < 0.001f)
            return;

        for (var i = 0; i < _frame.Length; i++)
        {
            var scaled = _frame[i] * gain;
            _frame[i] = (short) Math.Clamp(scaled, short.MinValue, short.MaxValue);
        }
    }

    private void CaptureUpdate()
    {
        if (!_talking || _mic == null || _encoder == null)
            return;

        // Drain every complete frame the capture device has buffered since the last update.
        while (_mic.AvailableSamples >= VoiceChatConstants.FrameSamples)
        {
            var read = _mic.Read(_frame);
            if (read < VoiceChatConstants.FrameSamples)
                break;

            ApplyInputGain();

            int length;
            try
            {
                length = _encoder.Encode(_frame, VoiceChatConstants.FrameSamples, _encoded);
            }
            catch (Exception e)
            {
                Log.Warning($"Voice: Opus encode failed: {e}");
                break;
            }

            if (length <= 0)
                continue;

            _net.ClientSendMessage(new MsgVoiceChunk
            {
                Whisper = _whispering,
                Sequence = _sequence++,
                Data = _encoded[..length],
            });
        }
    }

    // ------------------------------------------------------------------
    // Receive path
    // ------------------------------------------------------------------

    private void OnVoicePlayback(MsgVoicePlayback message)
    {
        if (!_cfg.GetCVar(CCVars.VoiceEnabled))
            return;

        if (!TryGetEntity(message.Speaker, out var speakerUid))
            return;

        var speaker = speakerUid.Value;

        if (!_streams.TryGetValue(speaker, out var stream))
        {
            var source = _audio.CreateBufferedAudioSource(VoiceBufferCount);
            if (source == null)
                return;

            source.SampleRate = VoiceChatConstants.SampleRate;
            source.Global = false;
            source.ReferenceDistance = 1f;
            source.RolloffFactor = 1f;

            var decoder = _audio.CreateOpusDecoder(VoiceChatConstants.SampleRate, VoiceChatConstants.Channels);

            stream = new VoiceStream(source, decoder, VoiceBufferCount);
            _streams[speaker] = stream;
        }

        // Drop frames that arrive out of order (the voice channel is unreliable).
        if (stream.HasSequence && !IsNewer(message.Sequence, stream.LastSequence))
            return;

        stream.LastSequence = message.Sequence;
        stream.HasSequence = true;
        stream.Whisper = message.Whisper;
        stream.LastReceived = _timing.RealTime;

        int decoded;
        try
        {
            decoded = stream.Decoder.Decode(message.Data, _decodeBuf, VoiceChatConstants.FrameSamples, false);
        }
        catch (Exception e)
        {
            Log.Warning($"Voice: Opus decode failed: {e}");
            return;
        }

        if (decoded <= 0)
            return;

        // Recycle any buffers that have finished playing, then grab a free one for this frame.
        stream.ReclaimProcessed();
        if (!stream.TryDequeueFreeBuffer(out var buf))
            return; // Playback is behind; drop this frame rather than grow latency.

        // Reinterpret PCM as ushort (identical bit pattern in an unchecked context) into the upload scratch.
        // Pass a 2x-length span to satisfy the engine's mono buffer convention. (MemoryMarshal is sandbox-forbidden.)
        for (var i = 0; i < decoded; i++)
            _uploadBuf[i] = (ushort) _decodeBuf[i];
        stream.Source.WriteBuffer(buf, _uploadBuf.AsSpan(0, decoded * 2));
        stream.QueueBuffer(buf);

        if (!stream.Source.Playing)
            stream.Source.StartPlaying();
    }

    private void PlaybackUpdate()
    {
        if (_streams.Count == 0)
            return;

        var now = _timing.RealTime;
        var volume = _cfg.GetCVar(CCVars.VoiceVolume);
        var range = _cfg.GetCVar(CCVars.VoiceRange);
        var whisperRange = _cfg.GetCVar(CCVars.VoiceWhisperRange);

        _toRemove.Clear();

        foreach (var (uid, stream) in _streams)
        {
            if (Deleted(uid) || (now - stream.LastReceived).TotalSeconds > VoiceChatConstants.PlaybackTimeoutSeconds)
            {
                _toRemove.Add(uid);
                continue;
            }

            // Keep recycling played buffers so a silent stream can be torn down cleanly.
            stream.ReclaimProcessed();

            stream.Source.Position = _transform.GetWorldPosition(uid);
            stream.Source.MaxDistance = stream.Whisper ? whisperRange : range;
            stream.Source.Gain = stream.Whisper ? volume * 0.75f : volume;
        }

        foreach (var uid in _toRemove)
        {
            if (_streams.Remove(uid, out var stream))
                stream.Dispose();
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        CaptureUpdate();
        PlaybackUpdate();
        UpdateTalkIndicator();
    }

    // ------------------------------------------------------------------
    // "You are talking" HUD indicator
    // ------------------------------------------------------------------

    private void UpdateTalkIndicator()
    {
        if (_talkIndicator == null)
        {
            // Don't build the HUD control until the player actually talks for the first time.
            if (!_talking)
                return;

            TryBuildTalkIndicator();
        }

        if (_talkIndicator != null)
            _talkIndicator.Visible = _talking;
    }

    private void TryBuildTalkIndicator()
    {
        try
        {
            _talkIndicator = new TextureRect
            {
                Texture = _sprite.Frame0(
                    new SpriteSpecifier.Rsi(new ResPath("Structures/Wallmounts/intercom.rsi"), "speaker")),
                Stretch = TextureRect.StretchMode.Keep,
                Visible = false,
            };

            _ui.PopupRoot.AddChild(_talkIndicator);
            LayoutContainer.SetPosition(_talkIndicator, new Vector2(16f, 250f));
        }
        catch (Exception e)
        {
            Log.Warning($"Voice: failed to create talking indicator: {e}");
            _talkIndicator = null;
        }
    }

    /// <summary>True if <paramref name="seq"/> is ahead of <paramref name="last"/> in modular ushort space.</summary>
    private static bool IsNewer(ushort seq, ushort last)
    {
        return (ushort) (seq - last) is > 0 and < 32768;
    }

    /// <summary>Per-speaker decode + streaming playback state.</summary>
    private sealed class VoiceStream
    {
        public readonly IBufferedAudioSource Source;
        public readonly IOpusDecoder Decoder;

        public TimeSpan LastReceived;
        public ushort LastSequence;
        public bool HasSequence;
        public bool Whisper;

        private readonly Queue<int> _free;
        // Reusable scratch for buffer-handle calls; the content sandbox forbids stackalloc (unverifiable IL).
        private readonly int[] _handleScratch;

        public VoiceStream(IBufferedAudioSource source, IOpusDecoder decoder, int bufferCount)
        {
            Source = source;
            Decoder = decoder;
            _handleScratch = new int[bufferCount];
            _free = new Queue<int>(bufferCount);
            for (var i = 0; i < bufferCount; i++)
                _free.Enqueue(i);
        }

        public void ReclaimProcessed()
        {
            var processed = Source.GetNumberOfBuffersProcessed();
            if (processed <= 0)
                return;

            var handles = _handleScratch.AsSpan(0, processed);
            Source.GetBuffersProcessed(handles);
            for (var i = 0; i < handles.Length; i++)
                _free.Enqueue(handles[i]);
        }

        public bool TryDequeueFreeBuffer(out int handle)
        {
            if (_free.Count > 0)
            {
                handle = _free.Dequeue();
                return true;
            }

            handle = -1;
            return false;
        }

        public void QueueBuffer(int handle)
        {
            _handleScratch[0] = handle;
            Source.QueueBuffers(_handleScratch.AsSpan(0, 1));
        }

        public void Dispose()
        {
            Source.Dispose();
        }
    }
}
