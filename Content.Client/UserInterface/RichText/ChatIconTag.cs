using System.Diagnostics.CodeAnalysis;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Renders an inline job icon: <c>[chaticon kind="jobicon" key="JobIconCaptain"]</c>. Used to show a speaker's
/// job icon before their name in radio. Hovering shows the job name. The key is a JobIconPrototype id (the same
/// icon an ID card carries); the legacy kind "job" with a JobPrototype key is still accepted for old replays.
/// </summary>
[UsedImplicitly]
public sealed partial class ChatIconTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public string Name => "chaticon";

    // Target inline size in virtual px. Matches Goobstation's 20px icon box.
    private const float IconSize = 20f;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("kind", out var kindParam) || !kindParam.TryGetString(out var kind))
            return false;
        if (!node.Attributes.TryGetValue("key", out var keyParam) || !keyParam.TryGetString(out var key))
            return false;

        JobIconPrototype? jobIcon;

        switch (kind)
        {
            case "jobicon":
                if (!_proto.TryIndex(key, out jobIcon))
                    return false;
                break;
            case "job": // legacy markup from before icons were read off the ID card
                if (!_proto.TryIndex<JobPrototype>(key, out var job)
                    || !_proto.TryIndex(job.Icon, out jobIcon))
                    return false;
                break;
            default:
                return false;
        }

        Texture texture;
        try
        {
            texture = _entMan.System<SpriteSystem>().Frame0(jobIcon.Icon);
        }
        catch
        {
            return false;
        }

        // Adapted from Goobstation's IconTag: declare a fixed layout box and stretch the texture to fill it.
        // This gives the icon a deterministic footprint that sizes consistently with surrounding chat text,
        // unlike TextureScale + StretchMode.Keep which leaves the control reporting its native (e.g. 8x8) size.
        control = new TextureRect
        {
            Texture = texture,
            SetWidth = IconSize,
            SetHeight = IconSize,
            Stretch = TextureRect.StretchMode.Scale,
            VerticalAlignment = Control.VAlignment.Center,
            ToolTip = jobIcon.LocalizedJobName,
            MouseFilter = Control.MouseFilterMode.Stop,
        };
        return true;
    }
}
