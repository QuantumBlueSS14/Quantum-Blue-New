using Content.Shared.QB.Slasher.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.QB.Slasher;

/// <summary>
/// Keeps the client-side effigy sprite opacity in sync with the hidden or revealed appearance state.
/// </summary>
public sealed class SlasherEffigyVisualsSystem : EntitySystem
{
    private const float HiddenEffigyOpacity = 0.5f;

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherEffigyComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<SlasherEffigyComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData(ent, SlasherEffigyVisuals.Status, out SlasherEffigyStatus status))
            return;

        _sprite.SetColor((ent.Owner, args.Sprite), status == SlasherEffigyStatus.Hidden
            ? Color.White.WithAlpha(HiddenEffigyOpacity)
            : Color.White);
    }
}