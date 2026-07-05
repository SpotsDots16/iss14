using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.EntityCensus;

[UsedImplicitly]
public sealed class EntityCensusEui : BaseEui
{
    private readonly EntityCensusWindow _window;

    public EntityCensusEui()
    {
        _window = new EntityCensusWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is EntityCensusEuiState s)
            _window.SetState(s);
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}
