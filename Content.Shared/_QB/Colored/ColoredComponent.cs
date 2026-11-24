using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.QB.Colored;

/// <summary>
/// Component that applies a simple color tint to an entity using a shader.
/// Much simpler than the full paint system - just for basic coloring.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ColoredComponent : Component
{
    /// <summary>
    /// The color to apply to the entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    /// <summary>
    /// The original color before this component was added (used for restoration)
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color BeforeColor = Color.White;

    /// <summary>
    /// Whether the coloring is currently enabled
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The shader to use for coloring. Defaults to Greyscale which works well for recoloring.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ShaderName = "Greyscale";
}

[Serializable, NetSerializable]
public enum ColoredVisuals : byte
{
    Colored,
}
