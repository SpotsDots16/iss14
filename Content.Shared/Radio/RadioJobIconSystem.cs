using Content.Shared.Access.Systems;
using Content.Shared.PAI;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Radio;

/// <summary>
/// Resolves the job icon shown next to a speaker's name in radio chat.
/// Ported from Goob-Station's RadioJobIconSystem: the icon comes from the ID card the speaker
/// carries (in hand or inside their PDA), not from their mind's job role.
/// </summary>
public sealed partial class RadioJobIconSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SharedIdCardSystem _idCardSystem = default!;

    // These are static vars rather than being inlined so that the YAML linter can verify that they actually exist.
    private static readonly ProtoId<JobIconPrototype> JobIconAI = new("JobIconStationAi");
    private static readonly ProtoId<JobIconPrototype> JobIconBorg = new("JobIconBorg");
    private static readonly ProtoId<JobIconPrototype> JobIconNoID = new("JobIconNoId");

    /// <summary>
    /// Gets the radio job icon displayed next to a player's name when sending a message over radio.
    /// </summary>
    /// <param name="ent">The entity making a radio message.</param>
    /// <param name="jobIcon">The prototype ID of <paramref name="ent"/>'s job icon; defaults to "JobIconNoId" when no ID card is found.</param>
    /// <returns>True if <paramref name="ent"/> should show a job icon at all.</returns>
    public bool TryGetJobIcon(EntityUid ent, [NotNullWhen(true)] out ProtoId<JobIconPrototype>? jobIcon)
    {
        // If they're an AI/borg/other silicon, they get to return early and skip the `StatusIconComponent` check.
        if (TryGetSiliconIcon(ent, out jobIcon))
            return true;

        // Only show a job icon in chat for entities who normally have one in-game.
        if (!HasComp<StatusIconComponent>(ent))
            return false;

        // Try to get the icon from their ID card, if they have one.
        if (TryGetEquippedIdJobIcon(ent, out jobIcon))
            return true;

        // No ID card found: show the 'No ID' icon.
        jobIcon = JobIconNoID;
        return true;
    }

    private bool TryGetSiliconIcon(EntityUid ent, [NotNullWhen(true)] out ProtoId<JobIconPrototype>? jobIcon)
    {
        if (HasComp<StationAiHeldComponent>(ent))
        {
            jobIcon = JobIconAI;
            return true;
        }

        if (HasComp<BorgChassisComponent>(ent)
            || HasComp<BorgBrainComponent>(ent)
            || HasComp<PAIComponent>(ent)) // pAIs don't have radio access, but they can still get picked up by an intercom.
        {
            jobIcon = JobIconBorg;
            return true;
        }

        jobIcon = null;
        return false;
    }

    private bool TryGetEquippedIdJobIcon(EntityUid ent, [NotNullWhen(true)] out ProtoId<JobIconPrototype>? jobIcon)
    {
        jobIcon = null;

        // Finds ID cards held in hand or inside the equipped PDA.
        if (!_accessReader.FindAccessItemsInventory(ent, out var items))
            return false;

        foreach (var item in items)
        {
            // Check if each item is an ID card, or if it's a PDA with an ID inside it.
            if (_idCardSystem.TryGetIdCard(item, out var idCard))
            {
                jobIcon = idCard.Comp.JobIcon;
                return true;
            }
        }

        return false;
    }
}
