using System.Linq;
using Content.Server.EUI;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Opens the admin ID permissions editor. Accepts a player username / user id (edits the ID card that
/// player is carrying), or the entity uid of an ID card directly.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed partial class IdPermissionsCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private EuiManager _euis = default!;

    public override string Command => "idpermissions";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("cmd-idpermissions-server"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-idpermissions-help"));
            return;
        }

        // Direct entity uid of an ID card (e.g. from examine / VV).
        if (EntityUid.TryParse(args[0], out var uid)
            && _entities.EntityExists(uid)
            && _entities.HasComponent<IdCardComponent>(uid))
        {
            Open(uid, admin);
            return;
        }

        // Otherwise resolve a player and edit the ID card they're carrying.
        var located = await _locator.LookupIdByNameOrIdAsync(args[0]);
        if (located == null)
        {
            shell.WriteError(Loc.GetString("cmd-idpermissions-invalid-player"));
            return;
        }

        if (!_players.TryGetSessionById(located.UserId, out var session)
            || session.AttachedEntity is not { } attached)
        {
            shell.WriteError(Loc.GetString("cmd-idpermissions-player-offline"));
            return;
        }

        if (!_entities.System<SharedIdCardSystem>().TryFindIdCard(attached, out var idCard))
        {
            shell.WriteError(Loc.GetString("cmd-idpermissions-no-id", ("player", located.Username)));
            return;
        }

        Open(idCard.Owner, admin);
    }

    private void Open(EntityUid idCard, Robust.Shared.Player.ICommonSession admin)
    {
        var ui = new IdPermissionsEui(idCard);
        _euis.OpenEui(ui, admin);
        ui.BuildState();
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-idpermissions-completion"));
        }

        return CompletionResult.Empty;
    }
}
