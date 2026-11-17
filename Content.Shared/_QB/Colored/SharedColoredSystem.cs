using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Shared.QB.Colored;

public abstract class SharedColoredSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColoredComponent, ComponentInit>(OnColoredInit);
        SubscribeLocalEvent<ColoredComponent, ExaminedEvent>(OnExamined);
    }

    private void OnColoredInit(EntityUid uid, ColoredComponent component, ComponentInit args)
    {
        UpdateAppearance(uid, component);
    }


    private void OnExamined(EntityUid uid, ColoredComponent component, ExaminedEvent args)
    {
        if (component.Enabled && component.Color != Color.White)
        {
            var colorName = GetColorName(component.Color);
            args.PushMarkup(Loc.GetString("colored-component-examine-colored", ("color", colorName)));
        }
    }


    /// <summary>
    /// Gets a human-readable name for a color
    /// </summary>
    private string GetColorName(Color color)
    {
        // Simple color name mapping - could be expanded
        if (color.RByte == 255 && color.GByte == 0 && color.BByte == 0)
            return "red";
        if (color.RByte == 0 && color.GByte == 255 && color.BByte == 0)
            return "green";
        if (color.RByte == 0 && color.GByte == 0 && color.BByte == 255)
            return "blue";
        if (color.RByte == 255 && color.GByte == 255 && color.BByte == 0)
            return "yellow";
        if (color.RByte == 255 && color.GByte == 0 && color.BByte == 255)
            return "magenta";
        if (color.RByte == 0 && color.GByte == 255 && color.BByte == 255)
            return "cyan";
        if (color.RByte == 0 && color.GByte == 0 && color.BByte == 0)
            return "black";

        // Default to hex representation
        return $"#{color.RByte:X2}{color.GByte:X2}{color.BByte:X2}".ToLowerInvariant();
    }

    /// <summary>
    /// Updates the visual appearance of the colored entity
    /// </summary>
    public virtual void UpdateAppearance(EntityUid uid, ColoredComponent? component = null)
    {
        // Implementation in derived systems
    }
}
