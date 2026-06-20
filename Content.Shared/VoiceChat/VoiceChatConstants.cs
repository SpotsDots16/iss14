namespace Content.Shared.VoiceChat;

/// <summary>
///     Audio format constants shared by the voice capture/encode (client) and decode/playback (client)
///     and transcription (server) paths. Encoder and decoder MUST agree on these.
/// </summary>
public static class VoiceChatConstants
{
    /// <summary>Sample rate in Hz. 48 kHz is Opus's native full-band rate.</summary>
    public const int SampleRate = 48000;

    /// <summary>Mono. Proximity voice is positioned by the spatial audio system, so a single channel is enough.</summary>
    public const int Channels = 1;

    /// <summary>Length of one encoded audio frame in milliseconds. A valid Opus frame size (2.5/5/10/20/40/60).</summary>
    public const int FrameMilliseconds = 20;

    /// <summary>Samples per channel in one frame (<see cref="SampleRate"/> * <see cref="FrameMilliseconds"/> / 1000).</summary>
    public const int FrameSamples = SampleRate / 1000 * FrameMilliseconds;

    /// <summary>Upper bound on the size of a single encoded Opus packet, used to size scratch buffers.</summary>
    public const int MaxEncodedFrameBytes = 4000;

    /// <summary>
    ///     How long (seconds) a speaker's playback stream lingers without new frames before it is torn down.
    /// </summary>
    public const float PlaybackTimeoutSeconds = 1.0f;
}
