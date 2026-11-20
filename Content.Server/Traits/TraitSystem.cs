
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Content.Shared.Body; // imp
using Content.Shared.Tag; // imp
using Content.Shared.Preferences; // QB
using Content.Server.Mind; // QB

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!; // imp
    [Dependency] private readonly MindSystem _mindSystem = default!; // QB
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!; // QB

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // // When the player is spawned in, add all trait components selected during character creation
    // private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    // {
    //     // Check if player's job allows to apply traits
    //     if (args.JobId == null ||
    //         !_prototypeManager.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
    //         !protoJob.ApplyTraits)
    //     {
    //         return;
    //     }

    //     foreach (var traitId in args.Profile.TraitPreferences)
    //     {
    //         if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
    //         {
    //             Log.Error($"No trait found with ID {traitId}!");
    //             return;
    //         }

    //         if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, args.Mob) ||
    //             _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, args.Mob))
    //             continue;

    //         // Add all components required by the prototype IMP: to the body or specified organ
    //         // imp start
    //         if (traitPrototype.Organ != null)
    //         {
    //             foreach (var organ in _bodySystem.GetBodyOrgans(args.Mob))
    //             {
    //                 if (traitPrototype.Organ is { } organTag && _tagSystem.HasTag(organ.Id, organTag))
    //                 {
    //                     EntityManager.AddComponents(organ.Id, traitPrototype.Components);
    //                 }
    //             }
    //         }
    //         else // imp end
    //         {
    //             if (traitPrototype.Components.Count > 0)
    //             EntityManager.AddComponents(args.Mob, traitPrototype.Components, false);
    //         }

    //         // Add all JobSpecials required by the prototype
    //         foreach (var special in traitPrototype.Specials)
    //         {
    //             special.AfterEquip(args.Mob);
    //         }

    //         // Add item required by the trait
    //         if (traitPrototype.TraitGear == null)
    //             continue;

    //         if (!TryComp(args.Mob, out HandsComponent? handsComponent))
    //             continue;

    //         var coords = Transform(args.Mob).Coordinates;
    //         var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
    //         _sharedHandsSystem.TryPickup(args.Mob,
    //             inhandEntity,
    //             checkActionBlocker: false,
    //             handsComp: handsComponent);
    //     }
    // }


    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows traits to be applied.
        if (args.JobId == null ||
            !_prototypeManager.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        ApplyTraits(args.Mob, args.Profile);
    }

    /// <summary>
    /// Applies the traits selected in a character profile to a spawned humanoid.
    /// </summary>
    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile)
    {
        foreach (var traitId in profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Error($"No trait found with ID {traitId}!");
                continue;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, mob) ||
                _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, mob))
                continue;

            // Skip traits if the player's mind has a blacklisted antagonist role. - QB
            if (traitPrototype.MindRoleBlacklist != null &&
                _mindSystem.TryGetMind(mob, out var mindId, out var mind) &&
                _roleSystem.MindHasRole((mindId, mind), traitPrototype.MindRoleBlacklist))
            {
                continue;
            }

            // Add components to the body or specified organ.
            if (traitPrototype.Organ != null)
            {
                if (TryComp<BodyComponent>(mob, out var body))
                {
                    foreach (var organ in body.Organs?.ContainedEntities ?? [])
                    {
                        if (_tagSystem.HasTag(organ, traitPrototype.Organ.Value))
                        {
                            EntityManager.AddComponents(organ, traitPrototype.Components);
                        }
                    }
                }
            }
            else if (traitPrototype.Components.Count > 0)
            {
                EntityManager.AddComponents(mob, traitPrototype.Components, false);
            }

            // Apply trait specials.
            foreach (var special in traitPrototype.Specials)
            {
                special.AfterEquip(mob);
            }

            // Give trait gear.
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(mob, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(mob).Coordinates;
            var inhandEntity = Spawn(traitPrototype.TraitGear, coords);

            _sharedHandsSystem.TryPickup(mob,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }
}