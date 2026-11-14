using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Humanoid;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!; // QB add

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        // var profile = args.Session != null
        //     ? _prefs.GetPreferences(args.Session.UserId).SelectedCharacter as HumanoidCharacterProfile
        //     : HumanoidCharacterProfile.RandomWithSpecies();

        //QB edit: load profile from preferences if available
        var preferences = args.Session != null
            ? _prefs.GetPreferences(args.Session.UserId)
            : null;

        //QB select profile for antagonist roles if available
        var profile = preferences != null
            ? preferences.SelectProfileForAntag(args.AntagRoles)
                ?? preferences.SelectProfileForAntag(args.FallbackRoles)
                ?? preferences.SelectedCharacter as HumanoidCharacterProfile
            : HumanoidCharacterProfile.RandomWithSpecies();

        // imp edit start, add random profile generation
        if (ent.Comp.RandomProfile)
            profile = HumanoidCharacterProfile.Random(false);
        // imp edit end

        if (profile?.Species is not { } speciesId || !_proto.Resolve(speciesId, out var species))
        {
            species = _proto.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies);
        }

        if (ent.Comp.SpeciesOverride != null
            && (ent.Comp.SpeciesOverrideBlacklist?.Contains(new ProtoId<SpeciesPrototype>(species.ID)) ?? false))
        {
            species = _proto.Index(ent.Comp.SpeciesOverride.Value);
        }

        args.Entity = Spawn(species.Prototype);
        _humanoid.LoadProfile(args.Entity.Value, profile?.WithSpecies(species.ID));

        // QB add: set entity name based on profile if available
        if (profile != null)
            _metaData.SetEntityName(args.Entity.Value, profile.Name);
    }
}
