// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 ThunderBear2006 <bearthunder06@gmail.com>
// SPDX-FileCopyrightText: 2026 AlinaNova21 <alina@alinanova.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later


using Content.Shared._Goobstation.Silicon.AiCameraWarping; // Goobstation
using Robust.Client.UserInterface;

namespace Content.Client._Goobstation.Silicon.AiCameraWarping.Ui; // Goobstation

public sealed class AiCameraWarpBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AiCameraWarpMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AiCameraWarpMenu>();
        _menu.OnCamWarpAction += SendAction;
        _menu.OnRefresh += SendRefresh;

        // QB - the camera map draws the grid the AI core is on
        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
            _menu.SetGrid(xform.GridUid);

        // QB - populate the list on open instead of waiting for a manual refresh
        SendRefresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CameraWarpBuiState msg)
            return;

        _menu?.Update(msg);
    }

    public void SendAction(CameraWarpActionEvent action)
    {
        SendMessage(new CameraWarpActionMessage(action));
    }

    public void SendRefresh()
    {
        SendMessage(new CameraWarpRefreshActionMessage());
    }
}