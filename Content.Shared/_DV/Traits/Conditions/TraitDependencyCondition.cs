using System.Linq;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Conditions;

public sealed partial class TraitDependencyCondition : BaseTraitCondition
{
    [DataField]
    public HashSet<ProtoId<TraitPrototype>> Conflicts = new();

    [DataField]
    public HashSet<ProtoId<TraitPrototype>> Requires = new();

    protected override bool EvaluateImplementation(TraitConditionContext ctx) =>
        CheckSelection(ctx.SelectedTraits);

    protected override bool? EvaluateSelectionImplementation(IReadOnlySet<ProtoId<TraitPrototype>> selected) =>
        CheckSelection(selected);

    private bool CheckSelection(IReadOnlySet<ProtoId<TraitPrototype>>? selected) =>
        Requires.All(id => selected?.Contains(id) == true) &&
        !Conflicts.Any(id => selected?.Contains(id) == true);

    public override string GetTooltip(IPrototypeManager proto, ILocalizationManager loc)
    {
        var lines = new List<string>();
        foreach (var id in Conflicts)
        {
            if (proto.TryIndex(id, out var trait))
                lines.Add(loc.GetString("trait-condition-trait-conflict", ("trait", loc.GetString(trait.Name))));
        }
        foreach (var id in Requires)
        {
            if (proto.TryIndex(id, out var trait))
                lines.Add(loc.GetString("trait-condition-trait-required", ("trait", loc.GetString(trait.Name))));
        }

        var requirements = string.Join("\n", lines);
        return Invert ? loc.GetString("trait-condition-inverted", ("requirements", requirements)) : requirements;
    }
}
