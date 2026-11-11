using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.Effects;

public sealed partial class SpawnItemInHandEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public EntProtoId Item = string.Empty;

    public override void Apply(TraitEffectContext ctx)
    {
        // TraitSystem handles this effect because picking up items needs server systems.
    }
}
