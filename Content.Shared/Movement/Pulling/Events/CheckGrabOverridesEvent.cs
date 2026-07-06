// Goobstation - Grab (new file, ported from Goob-Station)
using Content.Goobstation.Common.Grab;

namespace Content.Shared.Movement.Pulling.Events;

public sealed class CheckGrabOverridesEvent : EntityEventArgs
{
    public CheckGrabOverridesEvent(GrabStage stage)
    {
        Stage = stage;
    }

    public GrabStage Stage { get; set; }
}
