using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class EntityCensusCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euis = default!;

    public override string Command => "entitycensus";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("cmd-entitycensus-server"));
            return;
        }

        var ui = new EntityCensusEui();
        _euis.OpenEui(ui, admin);
    }
}
