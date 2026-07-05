using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical;

public sealed partial class VomitSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private ThirstSystem _thirst = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private SharedForensicsSystem _forensics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        // iss14: the restored body system has no organ event relay; stomach organs are
        // queried directly in Vomit() via SharedBodySystem instead.
    }

    private const float ChemMultiplier = 0.1f;

    private static readonly ProtoId<SoundCollectionPrototype> VomitCollection = "Vomit";

    private static readonly ProtoId<ReagentPrototype> VomitPrototype = "Vomit";  // TODO: Dehardcode vomit prototype

    private readonly SoundSpecifier _vomitSound = new SoundCollectionSpecifier(VomitCollection,
        AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));

    /// <summary>
    /// Empties a stomach organ's solution into the given vomit solution.
    /// </summary>
    private bool TryVomitSolution(Entity<StomachComponent, OrganComponent> ent, Solution vomitSolution)
    {
        if (!_solutionContainer.ResolveSolution(ent.Owner,
                StomachSystem.DefaultSolutionName,
                ref ent.Comp1.Solution,
                out var sol))
            return false;

        // Empty stomach solution into the new vomit solution
        vomitSolution.AddSolution(sol, _proto);
        sol.RemoveAllSolution();

        // Remind the stomach that it's empty.
        _solutionContainer.UpdateChemicals(ent.Comp1.Solution.Value);
        return true;
    }

    /// <summary>
    /// Make an entity vomit, if they have a stomach.
    /// </summary>
    public void Vomit(EntityUid uid, float thirstAdded = -40f, float hungerAdded = -40f, bool force = false)
    {
        // Vomit only if entity is alive
        // Ignore condition if force was set to true
        if (!force && _mobState.IsDead(uid))
            return;

        // TODO: Need decals
        var solution = new Solution();

        // iss14: query stomach organs directly instead of relaying an event through the body.
        var handled = false;
        foreach (var stomach in _body.GetBodyOrganEntityComps<StomachComponent>(uid))
        {
            if (TryVomitSolution(stomach, solution))
                handled = true;
        }

        // Keep the event so non-body entities can still contribute or handle vomiting.
        var ev = new TryVomitEvent(solution, force, handled);
        RaiseLocalEvent(uid, ref ev);

        if (!ev.Handled)
            return;

        // Vomiting makes you hungrier and thirstier
        if (TryComp<HungerComponent>(uid, out var hunger))
            _hunger.ModifyHunger(uid, hungerAdded, hunger);

        if (TryComp<ThirstComponent>(uid, out var thirst))
            _thirst.ModifyThirst(uid, thirst, thirstAdded);

        // It fully empties the stomach, this amount from the chem stream is relatively small
        var solutionSize = (MathF.Abs(thirstAdded) + MathF.Abs(hungerAdded)) / 6;

        // Apply a bit of slowdown
        _movementMod.TryUpdateMovementSpeedModDuration(uid, MovementModStatusSystem.VomitingSlowdown, TimeSpan.FromSeconds(solutionSize), 0.5f);

        // Adds a tiny amount of the chem stream from earlier along with vomit
        if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
        {
            var vomitAmount = solutionSize;

            // iss14: the restored bloodstream keeps chemicals in a separate chemstream solution;
            // take 10% of the chemicals removed from it, like the pre-Nubody vomit did.
            if (_solutionContainer.ResolveSolution(uid, bloodStream.ChemicalSolutionName, ref bloodStream.ChemicalSolution))
            {
                var vomitChemstreamAmount = _solutionContainer.SplitSolution(bloodStream.ChemicalSolution.Value, vomitAmount);
                vomitChemstreamAmount.ScaleSolution(ChemMultiplier);
                solution.AddSolution(vomitChemstreamAmount, _proto);
                vomitAmount -= (float)vomitChemstreamAmount.Volume;
            }

            // Makes a vomit solution the size of 90% of the chemicals removed from the chemstream
            solution.AddReagent(new ReagentId(VomitPrototype, _bloodstream.GetEntityBloodData(uid)), vomitAmount);
        }

        if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
        {
            _forensics.TransferDna(puddle, uid, false);
        }


        if (!_netManager.IsServer)
            return;

        // Force sound to play as spill doesn't work if solution is empty.
        _audio.PlayPvs(_vomitSound, uid);
        _popup.PopupEntity(Loc.GetString("disease-vomit", ("person", Identity.Entity(uid, EntityManager))), uid);
    }
}

[ByRefEvent]
public record struct TryVomitEvent(Solution Sol, bool Forced = false, bool Handled = false);
