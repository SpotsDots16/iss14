// Goobstation - Grab (ported from Goob-Station)
using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.GrabReleaseBind;

/// <summary>
/// This handle binding the resist grab key
/// </summary>
public sealed partial class GrabReleaseBindSystem : EntitySystem
{
    [Dependency] private PullingSystem _pullingSystem = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ResistGrab,
                InputCmdHandler.FromDelegate(HandleResistGrab, handle: false, outsidePrediction: false))
            .Register<GrabReleaseBindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<GrabReleaseBindSystem>();
    }

    private void HandleResistGrab(ICommonSession? session)
    {
        if (session?.AttachedEntity == null || !TryComp<PullableComponent>(session.AttachedEntity, out var pullable))
            return;

        var uid = session.AttachedEntity.Value;
        if (!_blocker.CanInteract(uid, null))
            return;

        _pullingSystem.TryStopPull(uid, pullable, uid);
    }
}
