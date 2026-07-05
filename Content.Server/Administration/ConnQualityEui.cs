using Content.Server.Administration.Managers;
using Content.Server.Administration.Performance;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;

namespace Content.Server.Administration;

/// <summary>
/// Server side of the connection quality admin window (<c>connquality</c>).
/// Per-player ping and connection info, refreshed once per second while open. Read-only.
/// </summary>
public sealed partial class ConnQualityEui : BaseEui
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _players = default!;

    private PerformanceMonitorSystem _monitor = default!;

    public ConnQualityEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
        _monitor = _entity.System<PerformanceMonitorSystem>();
        _monitor.OnSample += OnSample;
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();
        _admins.OnPermsChanged -= OnPermsChanged;
        _monitor.OnSample -= OnSample;
    }

    private void OnSample()
    {
        if (!CanView())
        {
            Close();
            return;
        }

        StateDirty();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !CanView())
            Close();
    }

    private bool CanView() => _admins.HasAdminFlag(Player, AdminFlags.Admin);

    public override EuiStateBase GetNewState()
    {
        var entries = new List<ConnQualityEntry>();
        var now = DateTime.UtcNow;

        foreach (var session in _players.Sessions)
        {
            var character = string.Empty;
            if (session.AttachedEntity is { } attached && _entity.TryGetComponent(attached, out MetaDataComponent? meta))
                character = meta.EntityName;

            var connectedFor = now - session.ConnectedTime.ToUniversalTime();
            if (connectedFor < TimeSpan.Zero)
                connectedFor = TimeSpan.Zero;

            entries.Add(new ConnQualityEntry(
                session.Name,
                character,
                session.Channel.Ping,
                session.Status.ToString(),
                connectedFor));
        }

        return new ConnQualityEuiState(entries);
    }
}
