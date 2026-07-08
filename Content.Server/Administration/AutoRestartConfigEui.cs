using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.ServerUpdates;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server.Administration;

/// <summary>
/// iss14: Server side of the scheduled auto-restart config admin window.
/// Edits the <see cref="AutoRestartManager"/>; scheduling state comes from <see cref="ServerUpdateManager"/>.
/// </summary>
public sealed partial class AutoRestartConfigEui : BaseEui
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private AutoRestartManager _config = default!;
    [Dependency] private ServerUpdateManager _updates = default!;

    public AutoRestartConfigEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _admins.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player)
            BuildState();
    }

    private bool CanView() => _admins.HasAdminFlag(Player, AdminFlags.Host);
    private bool CanEdit() => _admins.HasAdminFlag(Player, AdminFlags.Host);

    public override EuiStateBase GetNewState() => _state;

    private AutoRestartConfigEuiState _state = new(false, false, "03:00", 0, 0, false);

    public void BuildState()
    {
        if (!CanView())
        {
            Close();
            return;
        }

        _state = new AutoRestartConfigEuiState(
            CanEdit(),
            _config.Enabled,
            _config.Time,
            DateTime.Now.Ticks,
            _updates.NextAutoRestart?.Ticks ?? 0,
            _updates.AutoRestartPending);

        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!CanEdit())
            return;

        switch (msg)
        {
            case AutoRestartSetEnabledMessage m:
                _config.SetEnabled(m.Value);
                break;
            case AutoRestartSetTimeMessage m:
                _config.SetTime(m.Time);
                break;
            default:
                return;
        }

        BuildState();
    }
}
