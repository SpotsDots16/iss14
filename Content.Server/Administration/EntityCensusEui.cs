using Content.Server.Administration.Managers;
using Content.Server.Administration.Performance;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server.Administration;

/// <summary>
/// Server side of the entity census admin window (<c>entitycensus</c>).
/// Top entity prototypes by count with round/minute growth, refreshed every few seconds
/// while open. Read-only.
/// </summary>
public sealed partial class EntityCensusEui : BaseEui
{
    /// <summary>Refresh every this many one-second samples.</summary>
    private const int SampleInterval = 5;

    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IEntityManager _entity = default!;

    private PerformanceMonitorSystem _monitor = default!;
    private EntityCensusSystem _census = default!;

    private int _samples;

    public EntityCensusEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
        _monitor = _entity.System<PerformanceMonitorSystem>();
        _census = _entity.System<EntityCensusSystem>();
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

        if (++_samples % SampleInterval != 0)
            return;

        StateDirty();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !CanView())
            Close();
    }

    private bool CanView() => _admins.HasAdminFlag(Player, AdminFlags.Admin);

    public override EuiStateBase GetNewState() => _census.GetState();
}
