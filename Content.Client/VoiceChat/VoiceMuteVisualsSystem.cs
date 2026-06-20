using Content.Shared.CCVar;
using Content.Shared.StatusIcon.Components;
using Content.Shared.VoiceChat;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.VoiceChat;

/// <summary>
///     Client side of the "voice muted" indicator. Contributes the muted status icon for any entity that has a
///     <see cref="VoiceMutedComponent"/>, and reports the local player's own mute state (the voice-enabled
///     setting) to the server so it can toggle that component.
/// </summary>
public sealed partial class VoiceMuteVisualsSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _player = default!;

    private const string MutedIcon = "VoiceMutedIcon";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceMutedComponent, GetStatusIconsEvent>(OnGetStatusIcons);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);

        _cfg.OnValueChanged(CCVars.VoiceInputEnabled, OnInputEnabledChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.VoiceInputEnabled, OnInputEnabledChanged);
    }

    private void OnGetStatusIcons(EntityUid uid, VoiceMutedComponent component, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex<VoiceIconPrototype>(MutedIcon, out var proto))
            args.StatusIcons.Add(proto);
    }

    private void OnInputEnabledChanged(bool enabled)
    {
        SendMuteState(!enabled);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        // Re-sync our mute state onto each new body so the icon shows up after spawning/cloning/etc.
        SendMuteState(!_cfg.GetCVar(CCVars.VoiceInputEnabled));
    }

    private void SendMuteState(bool muted)
    {
        // No body yet -> nothing for the server to attach the component to; it'll be sent on attach.
        if (_player.LocalEntity == null)
            return;

        RaiseNetworkEvent(new VoiceMuteStateChangedEvent(muted));
    }
}
