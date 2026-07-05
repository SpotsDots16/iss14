using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class ConnQualityCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euis = default!;

    public override string Command => "connquality";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("cmd-connquality-server"));
            return;
        }

        var ui = new ConnQualityEui();
        _euis.OpenEui(ui, admin);
    }
}
