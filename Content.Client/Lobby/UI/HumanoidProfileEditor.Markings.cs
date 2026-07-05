using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void UpdateMarkings()
    {
        if (Profile == null)
        {
            return;
        }

        Markings.SetData(Profile.Appearance.Markings, Profile.Species,
            Profile.Sex, Profile.Appearance.SkinColor, Profile.Appearance.EyeColor
        );
    }

    private void OnMarkingChange(MarkingSet markings)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings.GetForwardEnumerator().ToList()));
        ReloadProfilePreview();
    }

    private void UpdateHairPickers()
    {
        if (Profile == null)
        {
            return;
        }

        var hairMarking = Profile.Appearance.HairStyleId == HairStyles.DefaultHairStyle
            ? new List<Marking>()
            : new() { new(Profile.Appearance.HairStyleId, new List<Color>() { Profile.Appearance.HairColor }) };

        var facialHairMarking = Profile.Appearance.FacialHairStyleId == HairStyles.DefaultFacialHairStyle
            ? new List<Marking>()
            : new() { new(Profile.Appearance.FacialHairStyleId, new List<Color>() { Profile.Appearance.FacialHairColor }) };

        HairStylePicker.UpdateData(
            hairMarking,
            Profile.Species,
            1);
        FacialHairPicker.UpdateData(
            facialHairMarking,
            Profile.Species,
            1);
    }

    private void UpdateCMarkingsHair()
    {
        if (Profile == null)
        {
            return;
        }

        // hair color
        Color? hairColor = null;
        if (Profile.Appearance.HairStyleId != HairStyles.DefaultHairStyle &&
            _markingManager.Markings.TryGetValue(Profile.Appearance.HairStyleId, out var hairProto)
        )
        {
            if (_markingManager.CanBeApplied(Profile.Species, Profile.Sex, hairProto, _prototypeManager))
            {
                if (_markingManager.MustMatchSkin(Profile.Species, HumanoidVisualLayers.Hair, out var _, _prototypeManager))
                {
                    hairColor = Profile.Appearance.SkinColor;
                }
                else
                {
                    hairColor = Profile.Appearance.HairColor;
                }
            }
        }

        if (hairColor != null)
        {
            Markings.HairMarking = new(Profile.Appearance.HairStyleId, new List<Color>() { hairColor.Value });
        }
        else
        {
            Markings.HairMarking = null;
        }
    }

    private void UpdateCMarkingsFacialHair()
    {
        if (Profile == null)
        {
            return;
        }

        // facial hair color
        Color? facialHairColor = null;
        if (Profile.Appearance.FacialHairStyleId != HairStyles.DefaultFacialHairStyle &&
            _markingManager.Markings.TryGetValue(Profile.Appearance.FacialHairStyleId, out var facialHairProto))
        {
            if (_markingManager.CanBeApplied(Profile.Species, Profile.Sex, facialHairProto, _prototypeManager))
            {
                if (_markingManager.MustMatchSkin(Profile.Species, HumanoidVisualLayers.Hair, out var _, _prototypeManager))
                {
                    facialHairColor = Profile.Appearance.SkinColor;
                }
                else
                {
                    facialHairColor = Profile.Appearance.FacialHairColor;
                }
            }
        }

        if (facialHairColor != null)
        {
            Markings.FacialHairMarking = new(Profile.Appearance.FacialHairStyleId, new List<Color>() { facialHairColor.Value });
        }
        else
        {
            Markings.FacialHairMarking = null;
        }
    }
}
