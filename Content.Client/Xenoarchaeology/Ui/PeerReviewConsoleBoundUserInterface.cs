using Content.Shared.Xenoarchaeology.Equipment;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Xenoarchaeology.Ui;

[UsedImplicitly]
public sealed class PeerReviewConsoleBoundUserInterface : BoundUserInterface
{
    private PeerReviewConsoleWindow? _window;

    public PeerReviewConsoleBoundUserInterface(
        EntityUid owner,
        Enum uiKey)
        : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PeerReviewConsoleWindow>();

        _window.PublishRequested += tier =>
            SendMessage(new PeerReviewConsolePublishMessage(tier));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null ||
            state is not PeerReviewConsoleUiState peerReviewState)
        {
            return;
        }

        _window.UpdateState(peerReviewState);
    }
}
