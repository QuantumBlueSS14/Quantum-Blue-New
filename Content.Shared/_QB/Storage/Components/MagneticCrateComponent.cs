using Content.Shared.QB.Storage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.QB.Storage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(MagneticCrateSystem))]
public sealed partial class MagneticCrateComponent : Component
{
	[DataField, AutoNetworkedField]
	public bool MagnetEnabled;
}
