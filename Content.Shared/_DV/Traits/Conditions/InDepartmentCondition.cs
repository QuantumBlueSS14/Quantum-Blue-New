using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Conditions;

public sealed partial class InDepartmentCondition : BaseTraitCondition
{
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department = string.Empty;

    protected override bool EvaluateImplementation(TraitConditionContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.JobId))
            return false;

        if (!ctx.Proto.TryIndex(Department, out var department))
            return false;

        return department.Roles.Contains(ctx.JobId);
    }

    public override string GetTooltip(IPrototypeManager proto, ILocalizationManager loc)
    {
        var deptName = Department.Id;
        var deptColor = "#ffffff";

        if (proto.TryIndex(Department, out var deptProto))
        {
            deptName = loc.GetString(deptProto.Name);
            deptColor = deptProto.Color.ToHex();
        }


        return Invert
            ? loc.GetString("trait-condition-department-not", ("department", deptName), ("color", deptColor))
            : loc.GetString("trait-condition-department-is", ("department", deptName), ("color", deptColor));
    }
}
