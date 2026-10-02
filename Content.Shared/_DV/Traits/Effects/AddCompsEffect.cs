using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Effects;

public sealed partial class AddCompsEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public override void Apply(TraitEffectContext ctx)
    {
        ctx.EntMan.AddComponents(ctx.Player, Components, removeExisting: false);
    }
}
