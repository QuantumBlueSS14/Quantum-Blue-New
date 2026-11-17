using Content.Shared.QB.Colored;

namespace Content.Server.QB.Colored;

public sealed class ColoredSystem : SharedColoredSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColoredComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, ColoredComponent component, ComponentStartup args)
    {
        EnsureComp<AppearanceComponent>(uid);
        UpdateAppearance(uid, component);
    }



    public override void UpdateAppearance(EntityUid uid, ColoredComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        _appearance.SetData(uid, ColoredVisuals.Colored, component.Enabled);
        Dirty(uid, component);

    }

    /// <summary>
    /// Sets the color of an entity. Creates ColoredComponent if it doesn't exist.
    /// </summary>
    public void SetColor(EntityUid uid, Color color, string? shaderName = null)
    {
        var component = EnsureComp<ColoredComponent>(uid);

        component.Color = color;
        if (shaderName != null)
            component.ShaderName = shaderName;

        Dirty(uid, component);
        UpdateAppearance(uid, component);
    }

    /// <summary>
    /// Removes coloring from an entity
    /// </summary>
    public void RemoveColor(EntityUid uid)
    {
        if (!TryComp<ColoredComponent>(uid, out var component))
            return;

        RemComp<ColoredComponent>(uid);
    }

    /// <summary>
    /// Toggles coloring on/off without removing the component
    /// </summary>
    public void ToggleColoring(EntityUid uid, bool enabled)
    {
        if (!TryComp<ColoredComponent>(uid, out var component))
            return;

        component.Enabled = enabled;
        Dirty(uid, component);
        UpdateAppearance(uid, component);
    }
}
