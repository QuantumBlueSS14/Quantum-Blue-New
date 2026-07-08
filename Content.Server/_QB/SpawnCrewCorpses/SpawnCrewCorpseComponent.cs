using Robust.Shared.Prototypes;

namespace Content.Server.QB.SpawnCrewCorpses;

[RegisterComponent]
public sealed partial class SpawnCrewCorpseComponent : Component
{
    [DataField]
    public int MinSpawnCount { get; set; } = 1;

    [DataField]
    public int MaxSpawnCount { get; set; } = 1;

    [DataField]
    public EntProtoId CorpsePrototype { get; set; } = "SalvageHumanCorpse";

    [DataField]
    public bool CloneAppearance { get; set; } = true;

    [DataField]
    public bool DistinctCrewPerBatch { get; set; } = true;

    [DataField]
    public string CorpseName { get; set; } = "unidentified corpse";
}
