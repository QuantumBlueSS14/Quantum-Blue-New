using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Effects;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class BaseTraitEffect
{
    [PublicAPI]
    public abstract void Apply(TraitEffectContext ctx);
}

public sealed class TraitEffectContext
{
    public required EntityUid Player { get; init; }
    public required IEntityManager EntMan { get; init; }
    public required IPrototypeManager Proto { get; init; }
    public required IComponentFactory CompFactory { get; init; }
    public required ILogManager LogMan { get; init; }
    public required TransformComponent Transform { get; init; }
}
