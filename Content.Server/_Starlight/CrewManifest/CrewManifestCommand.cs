// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from starlight-ss14. iss14 adaptations: Assistant -> Passenger fallback, no CustomSpecieName
// (not in this fork), and CreateGeneralRecord here requires a non-null profile so we pass the
// player's preference profile (or a species default).

using System.Linq;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Roles.Jobs;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._Starlight.CrewManifest;

[AdminCommand(AdminFlags.Fun)]
[ToolshedCommand]
public sealed class CrewManifestCommand : ToolshedCommand
{
    [Dependency] private IPlayerManager _plr = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    private StationRecordsSystem? _records;
    private JobSystem? _job;
    private MindSystem? _mind;
    private ContainerSystem? _container;
    private InventorySystem? _inventory;
    private static readonly ProtoId<JobPrototype> FallbackJob = "Passenger";

    [CommandImplementation("addto")]
    public EntityUid AddToManifest([PipedArgument] EntityUid uid, EntityUid station, bool useIdJob, bool addRole)
    {
        AddRecord(station, uid, useIdJob, addRole);
        return uid;
    }

    [CommandImplementation("addto")]
    public IEnumerable<EntityUid> AddToManifest([PipedArgument] IEnumerable<EntityUid> uids, EntityUid station, bool useIdJob, bool addRole) =>
        uids.Select(x => AddToManifest(x, station, useIdJob, addRole));

    [CommandImplementation("removefrom")]
    public EntityUid RemoveFromManifest([PipedArgument] EntityUid uid, EntityUid station)
    {
        RemoveRecord(station, uid);
        return uid;
    }

    [CommandImplementation("removefrom")]
    public IEnumerable<EntityUid> RemoveFromManifest([PipedArgument] IEnumerable<EntityUid> uids, EntityUid station) =>
        uids.Select(x => RemoveFromManifest(x, station));

    [CommandImplementation("addplayer")]
    public EntityUid AddPlayerToManifest([PipedArgument] EntityUid station, EntityUid uid, bool useIdJob, bool addRole)
    {
        AddRecord(station, uid, useIdJob, addRole);
        return station;
    }

    [CommandImplementation("addplayer")]
    public IEnumerable<EntityUid> AddPlayerToManifest([PipedArgument] IEnumerable<EntityUid> stations, EntityUid uid, bool useIdJob, bool addRole) =>
        stations.Select(x => AddPlayerToManifest(x, uid, useIdJob, addRole));

    [CommandImplementation("removeplayer")]
    public EntityUid RemovePlayerFromManifest([PipedArgument] EntityUid station, EntityUid uid)
    {
        RemoveRecord(station, uid);
        return station;
    }

    [CommandImplementation("removeplayer")]
    public IEnumerable<EntityUid> RemovePlayerFromManifest([PipedArgument] IEnumerable<EntityUid> stations, EntityUid uid) =>
        stations.Select(x => RemovePlayerFromManifest(x, uid));

    private ProtoId<JobPrototype> GetJobOrDefault(EntityUid player)
    {
        // Attempt to fetch job from current ID for convenience. Otherwise, this will forcefully set the player's job role to Passenger.
        _job ??= EntitySystemManager.GetEntitySystem<JobSystem>();
        _container ??= EntitySystemManager.GetEntitySystem<ContainerSystem>();
        _inventory ??= EntitySystemManager.GetEntitySystem<InventorySystem>();

        if (!_inventory.TryGetSlotEntity(player, "id", out var target))
            return FallbackJob;

        if (TryComp<PdaComponent>(target, out var pda) && pda.ContainedId is { } id &&
            TryComp<IdCardComponent>(id, out var card))
        {
            if (card.JobPrototype is not null)
                return card.JobPrototype.Value;

            // Best-effort: derive the job from the icon id (JobIconCaptain -> Captain). Can fail for
            // inconsistent icon names, in which case we fall back to Passenger.
            var iconId = card.JobIcon.Id;
            var parsed = iconId.Replace("Icon", "").Replace("Job", "");
            if (_proto.HasIndex<JobPrototype>(parsed))
                return _proto.Index<JobPrototype>(parsed);
        }

        return FallbackJob;
    }

    private void AddRecord(EntityUid station, EntityUid player, bool useIdJob, bool addRole)
    {
        _records ??= EntitySystemManager.GetEntitySystem<StationRecordsSystem>();
        _job ??= EntitySystemManager.GetEntitySystem<JobSystem>();
        _mind ??= EntitySystemManager.GetEntitySystem<MindSystem>();
        _inventory ??= EntitySystemManager.GetEntitySystem<InventorySystem>();

        if (!_plr.TryGetSessionByEntity(player, out var session) || !_mind.TryGetMind(session.UserId, out var mind) ||
            !TryComp<StationRecordsComponent>(station, out var records))
            return;

        _inventory.TryGetSlotEntity(player, "id", out var target);
        _job.MindTryGetJobId(mind.Value, out var jobId);
        if (useIdJob)
        {
            jobId = GetJobOrDefault(player);
            if (addRole)
                _job.MindAddJob(mind.Value, jobId.Value);
        }

        // iss14: CreateGeneralRecord throws on invalid job ids, so make sure we always pass a real one.
        if (jobId is null || !_proto.HasIndex<JobPrototype>(jobId.Value))
            jobId = FallbackJob;

        TryComp<HumanoidAppearanceComponent>(player, out var humanoid);
        TryComp<FingerprintComponent>(player, out var fingerprint);
        TryComp<DnaComponent>(player, out var dna);
        var name = MetaData(player).EntityName;
        var age = humanoid?.Age ?? 0;
        var gender = humanoid?.Gender ?? Gender.Epicene;
        var species = humanoid?.Species.Id ?? "Human";

        // iss14: the record-created event requires a non-null profile; prefer the player's own.
        var profile = _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile
                      ?? HumanoidCharacterProfile.DefaultWithSpecies(humanoid?.Species).WithName(name);

        _records.CreateGeneralRecord(station, target, name, age, species, gender, jobId.Value,
            fingerprint?.Fingerprint, dna?.DNA, profile, records);
    }

    private void RemoveRecord(EntityUid station, EntityUid player)
    {
        _records ??= EntitySystemManager.GetEntitySystem<StationRecordsSystem>();
        if (!TryComp<StationRecordsComponent>(station, out var records))
            return;

        if (_records.GetRecordByName(station, MetaData(player).EntityName, records) is not { } id)
            return;

        var key = new StationRecordKey(id, station);
        _records.RemoveRecord(key, records);
    }
}
