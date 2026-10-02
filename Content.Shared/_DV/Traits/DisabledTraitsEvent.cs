using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.Traits;

[Serializable, NetSerializable]
public sealed class DisabledTraitsEvent(Dictionary<ProtoId<TraitPrototype>, List<string>> disabledTraits)
    : EntityEventArgs
{
    public Dictionary<ProtoId<TraitPrototype>, List<string>> DisabledTraits = disabledTraits;
}
