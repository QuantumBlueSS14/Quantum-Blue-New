using Content.Shared.QB.Storage.Components;
using Content.Shared.Gravity;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.QB.Storage.Systems;

public sealed class MagneticCrateSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly string ToggleIcon = "/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagneticCrateComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerb);
        SubscribeLocalEvent<MagneticCrateComponent, IsWeightlessEvent>(OnIsWeightless);
    }

    /// <summary>
    /// Toggles the magnet on or off for the magnetic crate. When toggled, it updates the crate's weightless state based on whether the magnet is enabled and whether the crate is on a gravity-supporting grid or map. It also shows a popup message to the user indicating whether the magnet has been turned on or off.
    /// </summary>
    /// <param name="uid">The entity representing the magnetic crate.</param>
    /// <param name="user">The entity representing the user toggling the magnet.</param>
    private void ToggleMagnet(EntityUid uid, EntityUid user)
    {
        if (!TryComp(uid, out MagneticCrateComponent? component))
            return;

        component.MagnetEnabled = !component.MagnetEnabled;
        Dirty(uid, component);
        _gravity.RefreshWeightless(uid);

        _popup.PopupPredicted(Loc.GetString(component.MagnetEnabled
            ? "magnetic-crate-toggle-on-popup"
            : "magnetic-crate-toggle-off-popup"), user, user);
    }
    
    /// <summary> Adds an alternative verb to toggle the magnet on or off triggered by right-click menu or altclicking the crate.
    /// <param name="uid"> The entity representing the magnetic crate.</param>
    /// <param name="component"> The magnetic crate component.</param>
    /// <param name="verbArgs"> The event arguments for getting alternative verbs, which includes the user and the list of verbs to add to.</param>
    /// </summary>
    private void OnGetAltVerb(EntityUid uid, MagneticCrateComponent component, GetVerbsEvent<AlternativeVerb> verbArgs)
    {
        if (!verbArgs.CanAccess || !verbArgs.CanInteract)
            return;

        var user = verbArgs.User;
        verbArgs.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(component.MagnetEnabled //This is a lambda but it's a really simple one.
                ? "magnetic-crate-toggle-off-verb"
                : "magnetic-crate-toggle-on-verb"),
            Icon = new SpriteSpecifier.Texture(new(ToggleIcon)),
            Priority = 3,
            Act = () => ToggleMagnet(uid, user)
        });
    }

    /// <summary>
    /// Handles the IsWeightlessEvent for magnetic crates. If the magnet is enabled and the crate is on a gravity-supporting grid or map, it sets the crate to not be weightless, allowing it to stick to surfaces in zero-g environments.
    /// </summary>
    /// <param name="ent">The entity representing the magnetic crate.</param>
    /// <param name="weightlessEvent">The event arguments for checking if the entity is weightless.</param>
    private void OnIsWeightless(Entity<MagneticCrateComponent> ent, ref IsWeightlessEvent weightlessEvent)
    {
        if (weightlessEvent.Handled || !ent.Comp.MagnetEnabled)
            return;

        if (!_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        // This is what allows the crate to actually stick.
        if (ent.Comp.MagnetEnabled)
        {
            weightlessEvent.IsWeightless = false;
            weightlessEvent.Handled = true;
        }
    }
}
