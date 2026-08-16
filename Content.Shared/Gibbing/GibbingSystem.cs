using Content.Shared.Destructible;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared._QB.Gibbing.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Gibbing;

public sealed class GibbingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    /// <summary>
    /// Gibs an entity.
    /// </summary>
    /// <param name="ent">The entity to gib.</param>
    /// <param name="dropGiblets">Whether or not to drop giblets.</param>
    /// <param name="user">The user gibbing the entity, if any.</param>
    /// <returns>The set of giblets for this entity, if any.</returns>
    public HashSet<EntityUid> Gib(EntityUid ent, bool dropGiblets = true, EntityUid? user = null)
    {
        // user is unused because of prediction woes, eventually it'll be used for audio

        // BodySystem handles prediction rather poorly and causes client-sided bugs when we gib on the client
        // This guard can be removed once it is gone and replaced by a prediction-safe system.
        if (!_net.IsServer)
            return new();

        // QB Add- if this body's mind has any GibImmune role, cancel immediately,
        // but still raise the attempt event so existing redirect handlers (teleports, etc.) can run.
        if (HasGibImmuneMindRole(ent))
        {
            var roleImmuneAttempt = new AttemptEntityGibCancelEvent(ent)
            {
                Cancelled = true,
            };

            RaiseLocalEvent(ent, ref roleImmuneAttempt);
            return new();
        }

        //Global cancellation hook: keeps all gib entry points (including direct destructible gibs)
        // consistent with systems that need to redirect death behavior.
        var attemptEv = new AttemptEntityGibCancelEvent(ent);
        RaiseLocalEvent(ent, ref attemptEv);
        if (attemptEv.Cancelled)
            return new();
        // end QB Add

        if (!_destructible.DestroyEntity(ent))
            return new();

        _audio.PlayPvs(GibSound, ent);

        var gibbed = new HashSet<EntityUid>();
        var beingGibbed = new BeingGibbedEvent(gibbed);
        RaiseLocalEvent(ent, ref beingGibbed);

        if (dropGiblets)
        {
            foreach (var giblet in gibbed)
            {
                _transform.DropNextTo(giblet, ent);
                FlingDroppedEntity(giblet);
            }
        }

        var beforeDeletion = new GibbedBeforeDeletionEvent(gibbed);
        RaiseLocalEvent(ent, ref beforeDeletion);

        return gibbed;
    }

    private const float GibletLaunchImpulse = 8;
    private const float GibletLaunchImpulseVariance = 3;

    private void FlingDroppedEntity(EntityUid target)
    {
        var impulse = GibletLaunchImpulse + _random.NextFloat(GibletLaunchImpulseVariance);
        var scatterVec = _random.NextAngle().ToVec() * impulse;
        _physics.ApplyLinearImpulse(target, scatterVec);
    }

    // QB Add
    /// <summary>
    /// Returns true when the target's current mind owns at least one role with GibImmune.
    /// This allows role-based gib immunity to short-circuit destructive gib processing.
    /// </summary>
    private bool HasGibImmuneMindRole(EntityUid ent)
    {
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer)
            || mindContainer.Mind is not { } mindId
            || !TryComp<MindComponent>(mindId, out var mindComp))
        {
            return false;
        }

        foreach (var roleEnt in mindComp.MindRoleContainer.ContainedEntities)
        {
            if (HasComp<GibImmuneComponent>(roleEnt))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Raised on an entity when it is being gibbed.
/// </summary>
/// <param name="Giblets">If a component wants to provide giblets to scatter, add them to this hashset.</param>
[ByRefEvent]
public readonly record struct BeingGibbedEvent(HashSet<EntityUid> Giblets);

/// <summary>
/// Raised on an entity when it is about to be deleted after being gibbed.
/// </summary>
/// <param name="Giblets">The set of giblets this entity produced.</param>
[ByRefEvent]
public readonly record struct GibbedBeforeDeletionEvent(HashSet<EntityUid> Giblets);
