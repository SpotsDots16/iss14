using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server.Administration;

/// <summary>
/// iss14: Server side of the round-end advertisement config admin window.
/// Edits the <see cref="RoundEndAdManager"/>.
/// </summary>
public sealed partial class RoundEndAdConfigEui : BaseEui
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private RoundEndAdManager _config = default!;

    public RoundEndAdConfigEui()
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

    private bool CanView() => _admins.HasAdminFlag(Player, AdminFlags.Admin);
    private bool CanEdit() => _admins.HasAdminFlag(Player, AdminFlags.Admin);

    public override EuiStateBase GetNewState() => _state;

    private RoundEndAdConfigEuiState _state = new(false, false, "");

    public void BuildState()
    {
        if (!CanView())
        {
            Close();
            return;
        }

        _state = new RoundEndAdConfigEuiState(CanEdit(), _config.Enabled, _config.Message);
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!CanEdit())
            return;

        switch (msg)
        {
            case RoundEndAdSetEnabledMessage m:
                _config.SetEnabled(m.Value);
                break;
            case RoundEndAdSetMessageMessage m:
                _config.SetMessage(m.Message);
                break;
            default:
                return;
        }

        BuildState();
    }
}
