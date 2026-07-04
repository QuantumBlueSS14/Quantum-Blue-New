using Robust.Shared.Prototypes;

namespace Content.Shared.QB.EntityEffects.Components;

[RegisterComponent]
public sealed partial class OnDeathEntitySpawnComponent : Component
{
    [DataField]
    public EntProtoId EntityPrototype { get; set; } = "Smoke";

    [DataField]
    public int Radius { get; set; } = 1;
}
