// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 ThunderBear2006 <bearthunder06@gmail.com>
// SPDX-FileCopyrightText: 2026 AlinaNova21 <alina@alinanova.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Map; // QB
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Silicon.AiCameraWarping; // Goobstation

public abstract class SharedStationAiWarpSystem : EntitySystem { }

[Serializable, NetSerializable]
public sealed class CameraWarpActionEvent : EntityEventArgs
{
    public NetEntity Target;

    public CameraWarpActionEvent(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public record struct CameraWarpData()
{
    public string DisplayName = string.Empty;
    public NetEntity NetEntityUid = NetEntity.Invalid;

    /// <summary>
    /// QB - where the camera is, relative to the AI core's grid, for the camera map.
    /// </summary>
    public NetCoordinates Coordinates = NetCoordinates.Invalid;
}

[Serializable, NetSerializable]
public sealed class CameraWarpBuiState : BoundUserInterfaceState
{
    public List<CameraWarpData> CameraList;

    public CameraWarpBuiState(List<CameraWarpData> camList)
    {
        CameraList = camList;
    }
}

[Serializable, NetSerializable]
public sealed class CameraWarpActionMessage : BoundUserInterfaceMessage
{
    public readonly CameraWarpActionEvent? WarpAction;
    public CameraWarpActionMessage(CameraWarpActionEvent camWarpAction)
    {
        WarpAction = camWarpAction;
    }
}

[Serializable, NetSerializable]
public sealed class CameraWarpRefreshActionMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public enum CamWarpUiKey : byte
{
    Key
}