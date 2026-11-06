using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Server.Traits; // qb edit
using Content.Shared.Roles.Jobs; // qb edit
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Traits;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.TraitRandomizer;

public sealed partial class TraitRandomizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    // qb edit
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TraitSystem _traits = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    // qb edit end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitRandomizerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TraitRandomizerComponent> ent, ref MapInitEvent args)
    {
        // qb edit
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComponent) || !_playerManager.TryGetSessionById(mindComponent.UserId, out var session))
            return;

        var curProfile = (HumanoidCharacterProfile)_prefs.GetPreferences(session.UserId).SelectedCharacter;
        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>()
            .Where(trait => trait.Category is { } category && ent.Comp.Categories.Contains(category) &&
                            !curProfile.TraitPreferences.Contains(trait.ID))
            .ToList();
        // qb edit end

        // how many traits are we gonna get?
        var traitsToRoll = _random.Next(ent.Comp.MinTraits, ent.Comp.MaxTraits + 1);
        List<TraitPrototype> finalTraits = [];

        // pick a trait, ensure we don't pick it again, and add it to the final traits list. do this that many times.
        // note: currently this ignores points limits, because I think it's funnier that way.
        // qb edit
        for (var i = 0; i < traitsToRoll && traits.Count > 0; i++)
        {
            var thisTrait = _random.PickAndTake(traits);
            finalTraits.Add(thisTrait);
        }
        // qb edit end

        // qb edit
        _jobs.MindTryGetJob(mindId, out var job);
        foreach (var trait in finalTraits)
        {
            _traits.TryApplyTrait(ent, trait, job?.ID, session);
        }
        // qb edit end
    }
}
