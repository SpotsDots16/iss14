using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Master switch for the proximity voice-chat system. When false, clients will not
    ///     capture or play voice and the server drops any voice packets it receives.
    ///     Server-authoritative so admins can kill voice for the whole round.
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<bool> VoiceEnabled =
        CVarDef.Create("voice.enabled", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Range in tiles that normal (push-to-talk) speech carries.
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<float> VoiceRange =
        CVarDef.Create("voice.range", 7f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Range in tiles that whispered speech carries. Should be smaller than <see cref="VoiceRange"/>.
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<float> VoiceWhisperRange =
        CVarDef.Create("voice.whisper_range", 2f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Whether the server should run speech-to-text transcription on received voice and write it to the logs.
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<bool> VoiceTranscriptionEnabled =
        CVarDef.Create("voice.transcription_enabled", false, CVar.SERVER);

    /// <summary>
    ///     Client-side playback volume for received voice, 0-1.
    /// </summary>
    public static readonly CVarDef<float> VoiceVolume =
        CVarDef.Create("voice.volume", 1f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Gain applied to this client's own microphone before transmitting, 0-2 (1 = unchanged).
    /// </summary>
    public static readonly CVarDef<float> VoiceInputVolume =
        CVarDef.Create("voice.input_volume", 1f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Whether this client will capture and transmit microphone audio. Lets a player mute their own mic
    ///     without affecting playback of others.
    /// </summary>
    public static readonly CVarDef<bool> VoiceInputEnabled =
        CVarDef.Create("voice.input_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Name of the OpenAL capture (microphone) device to use. Empty string means the system default.
    /// </summary>
    public static readonly CVarDef<string> VoiceInputDevice =
        CVarDef.Create("voice.input_device", string.Empty, CVar.ARCHIVE | CVar.CLIENTONLY);
}
