using Content.Client.Humanoid;
using Content.Client.Lobby;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!EntMan.EntityExists(PreviewDummy) ||
            !EntMan.HasComponent<HumanoidAppearanceComponent>(PreviewDummy))
            return;

        EntMan.System<HumanoidAppearanceSystem>().LoadProfile(PreviewDummy, humanoid);
    }

    private void LoadHumanoidEntity(HumanoidCharacterProfile humanoid, JobPrototype? jobOverride, bool showClothes)
    {
        ProfileName = humanoid.Name;
        JobName = null;
        LoadoutName = null;

        var ui = UserInterfaceManager.GetUIController<LobbyUIController>();
        var job = jobOverride ?? ui.GetPreferredJob(humanoid);

        PreviewDummy = ui.LoadProfileEntity(humanoid, job, showClothes);

        if (job.ID != SharedGameTicker.FallbackOverflowJob)
        {
            JobName = job.LocalizedName;
            LoadoutName = ui.GetPreferredLoadoutName(humanoid, job);
        }
        else if (ui.GetPreferredAntag(humanoid) is { } antag)
        {
            JobName = Loc.GetString(antag.Name);
        }
    }
}
