using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.RoleReqs;

[UsedImplicitly]
public sealed class RoleReqEditorEui : BaseEui
{
    private readonly RoleReqEditorWindow _window;

    public RoleReqEditorEui()
    {
        _window = new RoleReqEditorWindow();

        _window.OnSetTimers += v => SendMessage(new RoleReqSetTimersEnabledMessage(v));
        _window.OnSetOverrides += v => SendMessage(new RoleReqSetOverridesEnabledMessage(v));
        _window.OnEditTime += (job, isAntag, idx, time) => SendMessage(new RoleReqEditTimeMessage(job, idx, time, isAntag));
        _window.OnSetInverted += (job, isAntag, idx, inv) => SendMessage(new RoleReqSetInvertedMessage(job, idx, inv, isAntag));
        _window.OnRemove += (job, isAntag, idx) => SendMessage(new RoleReqRemoveMessage(job, idx, isAntag));
        _window.OnAdd += (job, isAntag, kind, target, time, inv) => SendMessage(new RoleReqAddMessage(job, kind, target, time, inv, isAntag));
        _window.OnResetJob += (job, isAntag) => SendMessage(new RoleReqResetJobMessage(job, isAntag));
        _window.OnSaveProfile += name => SendMessage(new RoleReqSaveProfileMessage(name));
        _window.OnLoadProfile += name => SendMessage(new RoleReqLoadProfileMessage(name));
        _window.OnDeleteProfile += name => SendMessage(new RoleReqDeleteProfileMessage(name));
        _window.OnImport += () => SendMessage(new RoleReqImportPrototypeMessage());
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is RoleReqEditorState s)
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
