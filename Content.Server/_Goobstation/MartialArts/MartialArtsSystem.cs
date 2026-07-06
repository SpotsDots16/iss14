// Goobstation - MartialArts (ported from Goob-Station)
// iss14: Polymorph component copy handlers stripped (iss14 has no CopyPolymorphComponent helper).
using Content.Goobstation.Shared.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Events;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;

namespace Content.Goobstation.Server.MartialArts;

/// <summary>
/// Just handles carp sayings for now.
/// </summary>
public sealed partial class MartialArtsSystem : SharedMartialArtsSystem
{
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CanPerformComboComponent, SleepingCarpSaying>(OnSleepingCarpSaying);
    }

    private void OnSleepingCarpSaying(Entity<CanPerformComboComponent> ent, ref SleepingCarpSaying args)
    {
        _chat.TrySendInGameICMessage(ent, Loc.GetString(args.Saying), InGameICChatType.Speak, false);
    }
}
