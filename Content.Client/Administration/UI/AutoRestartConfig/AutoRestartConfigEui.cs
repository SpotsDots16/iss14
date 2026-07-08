using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.AutoRestartConfig;

[UsedImplicitly]
public sealed class AutoRestartConfigEui : BaseEui
{
    private readonly AutoRestartConfigWindow _window;

    public AutoRestartConfigEui()
    {
        _window = new AutoRestartConfigWindow();

        _window.OnSetEnabled += value => SendMessage(new AutoRestartSetEnabledMessage(value));
        _window.OnSetTime += time => SendMessage(new AutoRestartSetTimeMessage(time));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is AutoRestartConfigEuiState s)
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
