using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.QB.Colored;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.QB.Colored;

public sealed class ColoredVisualizerSystem : VisualizerSystem<ColoredComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColoredComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ColoredComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ColoredComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<ColoredComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<ColoredComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnInit(EntityUid uid, ColoredComponent component, ComponentInit args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnAfterHandleState(EntityUid uid, ColoredComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, component);

        // This is the key! Force refresh of equipment/held visuals
        // Same approach as ToggleableVisualsSystem
        _itemSystem.VisualsChanged(uid);
    }


    protected override void OnAppearanceChange(EntityUid uid, ColoredComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !_appearance.TryGetData(uid, ColoredVisuals.Colored, out bool isColored))
            return;

        if (!isColored)
            return;

        var shader = _protoMan.Index<ShaderPrototype>(component.ShaderName).Instance();
        foreach (var spriteLayer in args.Sprite.AllLayers)
        {
            if (spriteLayer is not Layer layer)
                continue;

            if (layer.Shader == null || layer.Shader == shader)
            {
                layer.Shader = shader;
                layer.Color = component.Color;
            }
        }
    }

    private void UpdateAppearance(EntityUid uid, ColoredComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false) || !component.Enabled)
            return;

        if (!_protoMan.TryIndex<ShaderPrototype>(component.ShaderName, out var shaderProto))
            return;

        var shader = shaderProto.Instance();

        foreach (var spriteLayer in sprite.AllLayers)
        {
            if (spriteLayer is not Layer layer)
                continue;

            // Only apply to layers that don't already have a different shader
            if (layer.Shader == null || layer.Shader == shader)
            {
                layer.Shader = shader;
                layer.Color = component.Color;
            }
        }
    }

    private void OnShutdown(EntityUid uid, ColoredComponent component, ref ComponentShutdown args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (Terminating(uid))
            return;

        if (!_protoMan.TryIndex<ShaderPrototype>(component.ShaderName, out var shaderProto))
            return;

        var shader = shaderProto.Instance();

        // Restore original appearance
        foreach (var spriteLayer in sprite.AllLayers)
        {
            if (spriteLayer is not Layer layer || layer.Shader != shader)
                continue;

            layer.Shader = null;
            if (layer.Color == component.Color)
                layer.Color = component.BeforeColor;
        }
    }

    private void OnHeldVisualsUpdated(EntityUid uid, ColoredComponent component, HeldVisualsUpdatedEvent args)
    {
        UpdateVisuals(component, args);
    }

    private void OnEquipmentVisualsUpdated(EntityUid uid, ColoredComponent component, EquipmentVisualsUpdatedEvent args)
    {
        UpdateVisuals(component, args);
    }

    private void UpdateVisuals(ColoredComponent component, EntityEventArgs args)
    {
        if (!component.Enabled)
            return;

        var layers = new HashSet<string>();
        var entity = EntityUid.Invalid;

        switch (args)
        {
            case HeldVisualsUpdatedEvent hgs:
                layers = hgs.RevealedLayers;
                entity = hgs.User;
                break;
            case EquipmentVisualsUpdatedEvent eqs:
                layers = eqs.RevealedLayers;
                entity = eqs.Equipee;
                break;
        }

        if (layers.Count == 0 || !TryComp(entity, out SpriteComponent? sprite))
            return;

        foreach (var revealed in layers)
        {
            if (!SpriteSystem.LayerMapTryGet((entity, sprite), revealed, out var layer, false))
                continue;

            sprite.LayerSetShader(layer, component.ShaderName);
            SpriteSystem.LayerSetColor((entity, sprite), layer, component.Color);
        }
    }
}
