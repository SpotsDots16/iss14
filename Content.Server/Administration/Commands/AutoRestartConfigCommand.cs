using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed partial class AutoRestartConfigCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euis = default!;

    public override string Command => "autorestartconfig";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("cmd-autorestartconfig-server"));
            return;
        }

        var ui = new AutoRestartConfigEui();
        _euis.OpenEui(ui, admin);
        ui.BuildState();
    }
}
