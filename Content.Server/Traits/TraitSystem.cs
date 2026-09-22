using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Content.Server.Body.Systems; // imp
using Content.Shared.Tag; // imp
// qb edit
using System.Linq;
using Content.Shared._DV.CCVars;
using Content.Shared._DV.Traits;
using Content.Shared._DV.Traits.Conditions;
using Content.Shared._DV.Traits.Effects;
using Content.Shared._QB.Traits;
using Content.Shared.Humanoid;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Content.Shared.Preferences;
// qb edit end

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!; // imp
    [Dependency] private readonly TagSystem _tagSystem = default!; // imp
    // qb edit
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ILogManager _log = default!;
    // qb edit end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // qb edit
    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        ApplyTraits(args.Mob, args.Profile, args.JobId, args.Player);
    }

    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile, string? jobId, ICommonSession? session = null)
    {
        // Check if player's job allows to apply traits
        if (jobId == null ||
            !_prototypeManager.Resolve<JobPrototype>(jobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        // Check conditions before counting points: an ineligible drawback must not
        // pay for traits that will actually be applied.
        var traits = TraitSelection.Validate(profile.TraitPreferences, _prototypeManager,
            _config.GetCVar(DCCVars.MaxTraitCount), _config.GetCVar(DCCVars.MaxTraitPoints),
            (trait, selected) => CanApply(mob, trait, CreateConditionContext(mob, jobId, session, selected)));
        var selectedTraits = traits.ToHashSet();
        var disabled = new Dictionary<ProtoId<TraitPrototype>, List<string>>();
        var context = CreateConditionContext(mob, jobId, session, selectedTraits);
        foreach (var id in profile.TraitPreferences.Except(traits))
        {
            if (!_prototypeManager.TryIndex(id, out var trait))
                continue;

            var reasons = trait.Conditions.Where(condition => !condition.Evaluate(context))
                .Select(condition => condition.GetTooltip(_prototypeManager, Loc))
                .Where(reason => !string.IsNullOrEmpty(reason)).ToList();
            if (trait.ExcludedSpecies.Any(species => species.Id == context.SpeciesId))
                reasons.Add(Loc.GetString("trait-species-excluded-tooltip"));
            if (reasons.Count == 0)
                reasons.Add(Loc.GetString("disabled-traits-reason-unavailable"));
            disabled[id] = reasons;
        }

        foreach (var trait in traits.Select(id => _prototypeManager.Index(id)).OrderByDescending(trait => trait.Priority))
            ApplyTrait(mob, trait);

        if (session != null && disabled.Count > 0)
            RaiseNetworkEvent(new DisabledTraitsEvent(disabled), session);
    }

    // Randomized traits use the same effects, but intentionally ignore point limits.
    public bool TryApplyTrait(EntityUid mob, TraitPrototype trait, string? jobId, ICommonSession? session,
        IReadOnlySet<ProtoId<TraitPrototype>>? selectedTraits = null)
    {
        var context = CreateConditionContext(mob, jobId, session, selectedTraits);
        if (!CanApply(mob, trait, context))
            return false;

        ApplyTrait(mob, trait);
        return true;
    }

    private TraitConditionContext CreateConditionContext(EntityUid mob, string? jobId, ICommonSession? session,
        IReadOnlySet<ProtoId<TraitPrototype>>? selectedTraits)
    {
        return new TraitConditionContext
        {
            Player = mob,
            Session = session,
            EntMan = EntityManager,
            Proto = _prototypeManager,
            CompFactory = _factory,
            LogMan = _log,
            JobId = jobId,
            SpeciesId = TryComp<HumanoidAppearanceComponent>(mob, out var humanoid) ? humanoid.Species.Id : null,
            SelectedTraits = selectedTraits,
        };
    }

    private void ApplyTrait(EntityUid mob, TraitPrototype trait)
    {
        // Add all components required by the prototype IMP: to the body or specified organ
        // imp start
        if (trait.Organ != null)
        {
            foreach (var organ in _bodySystem.GetBodyOrgans(mob))
            {
                if (trait.Organ is { } organTag && _tagSystem.HasTag(organ.Id, organTag))
                {
                    EntityManager.AddComponents(organ.Id, trait.Components);
                }
            }
        }
        else // imp end
        {
            if (trait.Components.Count > 0)
                EntityManager.AddComponents(mob, trait.Components, false);
        }

        // Add all JobSpecials required by the prototype
        foreach (var special in trait.Specials)
        {
            special.AfterEquip(mob);
        }

        ApplyEffects(mob, trait);

        // Add item required by the trait
        if (trait.TraitGear == null)
            return;

        if (!TryComp(mob, out HandsComponent? handsComponent))
            return;

        var coords = Transform(mob).Coordinates;
        var inhandEntity = Spawn(trait.TraitGear, coords);
        _sharedHandsSystem.TryPickup(mob,
            inhandEntity,
            checkActionBlocker: false,
            handsComp: handsComponent);
    }

    private bool CanApply(EntityUid mob, TraitPrototype trait, TraitConditionContext context)
    {
        return !_whitelistSystem.IsWhitelistFail(trait.Whitelist, mob) &&
               !_whitelistSystem.IsWhitelistPass(trait.Blacklist, mob) &&
               !trait.ExcludedSpecies.Any(species => species.Id == context.SpeciesId) &&
               trait.Conditions.All(condition => condition.Evaluate(context));
    }

    private void ApplyEffects(EntityUid mob, TraitPrototype trait)
    {
        var context = new TraitEffectContext
        {
            Player = mob,
            EntMan = EntityManager,
            Proto = _prototypeManager,
            CompFactory = _factory,
            LogMan = _log,
            Transform = Transform(mob),
        };

        foreach (var effect in trait.Effects)
        {
            if (effect is SpawnItemInHandEffect spawn)
            {
                var item = Spawn(spawn.Item, context.Transform.Coordinates);
                _sharedHandsSystem.TryPickup(mob, item, checkActionBlocker: false);
            }
            else
            {
                effect.Apply(context);
            }
        }
    }
    // qb edit end
}
