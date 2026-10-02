using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._DV.Traits.Conditions;

public sealed partial class HasCompCondition : BaseTraitCondition
{
    [DataField(required: true, customTypeSerializer: typeof(ComponentNameSerializer))]
    public string Component = string.Empty;

    [DataField]
    public LocId? Tooltip;

    protected override bool EvaluateImplementation(TraitConditionContext ctx)
    {
        if (string.IsNullOrEmpty(Component))
            return false;

        return ctx.CompFactory.TryGetRegistration(Component, out var registration) &&
               ctx.EntMan.HasComponent(ctx.Player, registration.Type);
    }

    public override string GetTooltip(IPrototypeManager proto, ILocalizationManager loc)
    {
        return Tooltip is { } tooltip ? loc.GetString(tooltip) : string.Empty;
    }
}
