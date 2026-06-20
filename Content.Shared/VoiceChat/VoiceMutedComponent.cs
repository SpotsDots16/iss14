using Robust.Shared.GameStates;

namespace Content.Shared.VoiceChat;

/// <summary>
///     Present on an entity whose player has disabled proximity voice chat. Networked so every client can
///     render a "voice muted" status icon over the player's head. Presence is the entire state.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoiceMutedComponent : Component;
