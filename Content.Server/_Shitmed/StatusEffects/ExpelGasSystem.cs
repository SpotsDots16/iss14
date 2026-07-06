// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.StatusEffects;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes; // iss14
using Robust.Shared.Prototypes; // iss14
using Robust.Shared.Random;

namespace Content.Server._Shitmed.StatusEffects;

public sealed partial class ExpelGasEffectSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!; // iss14

    private static readonly ProtoId<EmotePrototype> FartEmote = "Fart"; // iss14: RA0033

    public override void Initialize()
    {
        SubscribeLocalEvent<ExpelGasComponent, ComponentInit>(OnInit);
    }
    private void OnInit(EntityUid uid, ExpelGasComponent component, ComponentInit args)
    {
        var mix = _atmos.GetContainingMixture((uid, Transform(uid)), true, true) ?? new();
        var gas = _random.Pick(component.PossibleGases);
        mix.AdjustMoles(gas, 60);
        // iss14: the Goob "Fart" emote prototype isn't ported to this fork; guard so the effect still works without it.
        if (_proto.HasIndex(FartEmote))
            _chat.TryEmoteWithChat(uid, FartEmote);
    }


}
