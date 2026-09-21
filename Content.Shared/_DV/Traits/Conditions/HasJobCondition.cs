using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Conditions;

public sealed partial class HasJobCondition : BaseTraitCondition
{
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job = string.Empty;

    protected override bool EvaluateImplementation(TraitConditionContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.JobId))
            return false;

        return ctx.JobId == Job;
    }

    public override string GetTooltip(IPrototypeManager proto, ILocalizationManager loc)
    {
        var jobName = Job.Id;
        var jobColor = "#ffffff";

        if (proto.TryIndex(Job, out var jobProto))
        {
            jobName = loc.GetString(jobProto.Name);

            foreach (var dept in proto.EnumeratePrototypes<DepartmentPrototype>())
            {
                if (dept.Roles.Contains(Job))
                {
                    jobColor = dept.Color.ToHex();
                    break;
                }
            }
        }

        return Invert
            ? loc.GetString("trait-condition-job-not", ("job", jobName), ("color", jobColor))
            : loc.GetString("trait-condition-job-is", ("job", jobName), ("color", jobColor));
    }
}
