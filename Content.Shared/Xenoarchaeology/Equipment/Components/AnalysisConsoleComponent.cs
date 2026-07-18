using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;


/// The console that is used for artifact analysis

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AnalysisConsoleComponent : Component
{

    /// The analyzer entity the console is linked.
    /// Can be null if not linked.

    [DataField, AutoNetworkedField]
    public EntityUid? AnalyzerEntity;

    [DataField]
    public SoundSpecifier? ScanFinishedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");


    /// The sound played when an artifact has points extracted.

    [DataField]
    public SoundSpecifier? ExtractSound = new SoundPathSpecifier("/Audio/Effects/radpulse11.ogg")
    {
        Params = new AudioParams
        {
            Volume = 4,
        }
    };


    /// Sound played when the analyzer produces its research printouts.

    [DataField]
    public SoundSpecifier PrintoutSound = default!;


    /// Delay between extracting research and producing the printouts.

    [DataField]
    public TimeSpan PrintoutDelay = TimeSpan.FromSeconds(0.25);

    /// The machine linking port for linking the console with the analyzer.

    [DataField]
    public ProtoId<SourcePortPrototype> LinkingPort = "ArtifactAnalyzerSender";
}

[Serializable, NetSerializable]
public enum ArtifactAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AnalysisConsoleExtractButtonPressedMessage : BoundUserInterfaceMessage;

