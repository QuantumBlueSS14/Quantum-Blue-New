using System;
using System.Collections.Generic;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView : SpriteView
{
    private IClientPreferencesManager _preferencesManager = default!;
    private IPrototypeManager _prototypeManager = default!;
    private ISharedPlayerManager _playerManager = default!;
    private MetaDataSystem _metaDataSystem = default!;

    public string? ProfileName { get; private set; }
    public string? JobName { get; private set; }
    public string? LoadoutName { get; private set; }
    public string? FullDescription { get; private set; }
    public EntityUid PreviewDummy { get; private set; } = EntityUid.Invalid;

    public void Initialize(
        IClientPreferencesManager prefMan,
        IPrototypeManager protoMan,
        ISharedPlayerManager playerMan)
    {
        _preferencesManager = prefMan;
        _prototypeManager = protoMan;
        _playerManager = playerMan;
        _metaDataSystem = EntMan.System<MetaDataSystem>();
    }

    public void LoadPreview(ICharacterProfile profile, JobPrototype? jobOverride = null, bool showClothes = true)
    {
        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        if (profile is not HumanoidCharacterProfile humanoid)
            throw new ArgumentException("Only humanoid profiles are implemented in ProfilePreviewSpriteView");

        LoadHumanoidEntity(humanoid, jobOverride, showClothes);
        FullDescription = ConstructFullDescription();
        SetEntity(PreviewDummy);
        InvalidateMeasure();
        _metaDataSystem.SetEntityName(PreviewDummy, profile.Name);
    }

    public void ReloadProfilePreview(ICharacterProfile profile)
    {
        if (profile is not HumanoidCharacterProfile humanoid)
            throw new ArgumentException("Only humanoid profiles are implemented in ProfilePreviewSpriteView");

        ReloadHumanoidEntity(humanoid);
    }

    private string ConstructFullDescription()
    {
        var lines = new List<string>();
        if (ProfileName != null)
            lines.Add(ProfileName);
        if (LoadoutName != null)
            lines.Add($"\"{LoadoutName}\"");
        if (JobName != null)
            lines.Add(JobName);

        return string.Join("\n", lines);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
    }
}
