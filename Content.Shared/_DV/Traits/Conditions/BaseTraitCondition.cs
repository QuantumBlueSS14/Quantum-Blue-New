using JetBrains.Annotations;
using Content.Shared.Traits;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Conditions;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class BaseTraitCondition
{
    [DataField]
    public bool Invert;

    [PublicAPI]
    public bool Evaluate(TraitConditionContext ctx)
    {
        var result = EvaluateImplementation(ctx);
        return result ^ Invert;
    }

    [PublicAPI]
    public abstract string GetTooltip(IPrototypeManager proto, ILocalizationManager loc);

    protected abstract bool EvaluateImplementation(TraitConditionContext ctx);

    // Null means this condition needs information that profile validation does not have.
    public bool? EvaluateSelection(IReadOnlySet<ProtoId<TraitPrototype>> selected)
    {
        var result = EvaluateSelectionImplementation(selected);
        return result.HasValue ? result.Value ^ Invert : null;
    }

    protected virtual bool? EvaluateSelectionImplementation(IReadOnlySet<ProtoId<TraitPrototype>> selected) => null;
}

public sealed class TraitConditionContext
{
    public required EntityUid Player { get; init; }
    public required ICommonSession? Session { get; init; }
    public required IEntityManager EntMan { get; init; }
    public required IPrototypeManager Proto { get; init; }
    public required IComponentFactory CompFactory { get; init; }
    public required ILogManager LogMan { get; init; }

    public string? JobId { get; init; }

    public string? SpeciesId { get; init; }

    public IReadOnlySet<ProtoId<TraitPrototype>>? SelectedTraits { get; init; }
}
