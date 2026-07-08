using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.RoundEndAdConfig;

[UsedImplicitly]
public sealed class RoundEndAdConfigEui : BaseEui
{
    private readonly RoundEndAdConfigWindow _window;

    public RoundEndAdConfigEui()
    {
        _window = new RoundEndAdConfigWindow();

        _window.OnSetEnabled += value => SendMessage(new RoundEndAdSetEnabledMessage(value));
        _window.OnSetMessage += message => SendMessage(new RoundEndAdSetMessageMessage(message));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is RoundEndAdConfigEuiState s)
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
