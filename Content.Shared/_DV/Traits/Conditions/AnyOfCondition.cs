using System.Linq;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Conditions;

public sealed partial class AnyOfCondition : BaseTraitCondition
{
    [DataField(required: true)]
    public List<BaseTraitCondition> Conditions = new();

    protected override bool EvaluateImplementation(TraitConditionContext ctx) =>
        Conditions.Any(condition => condition.Evaluate(ctx));

    protected override bool? EvaluateSelectionImplementation(IReadOnlySet<ProtoId<TraitPrototype>> selected)
    {
        var unknown = false;
        foreach (var condition in Conditions)
        {
            var result = condition.EvaluateSelection(selected);
            if (result == true)
                return true;
            unknown |= result == null;
        }

        return unknown ? null : false;
    }

    public override string GetTooltip(IPrototypeManager proto, ILocalizationManager loc)
    {
        var requirements = Conditions.Select(condition => condition.GetTooltip(proto, loc))
            .Where(tooltip => !string.IsNullOrEmpty(tooltip));
        return loc.GetString(Invert ? "trait-condition-none-of" : "trait-condition-any-of",
            ("requirements", string.Join("\n• ", requirements)));
    }
}
