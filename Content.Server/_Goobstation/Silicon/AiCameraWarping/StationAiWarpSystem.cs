// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 ThunderBear2006 <bearthunder06@gmail.com>
// SPDX-FileCopyrightText: 2026 AlinaNova21 <alina@alinanova.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq; // QB
using System.Numerics; // QB
using Content.Shared._Goobstation.Silicon.AiCameraWarping; // Goobstation
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Server.GameObjects;
using Robust.Shared.Map; // QB

namespace Content.Server._Goobstation.Silicon.AiCameraWarping; // Goobstation

public sealed class StationAiWarpSystem : SharedStationAiWarpSystem
{
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiHeldComponent, CameraWarpActionMessage>(OnUiWarpAction);
        SubscribeLocalEvent<StationAiHeldComponent, CameraWarpRefreshActionMessage>(OnUiRefreshRequested);
    }

    private void OnUiRefreshRequested(Entity<StationAiHeldComponent> ent, ref CameraWarpRefreshActionMessage args)
    {
        if (!_stationAiSystem.TryGetCore(ent.Owner, out var core))
            return;

        var cameras = GetCameras(core.Owner);

        SendCameras(ent.Owner, cameras); // QB - was an inline SetUiState
    }

    // QB edit start - keep open camera maps current, since cameras can move (bodycams) or go offline
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<EntityUid, List<CameraWarpData>> _lastSent = new();
    private readonly List<EntityUid> _closed = new();
    private TimeSpan _accumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += TimeSpan.FromSeconds(frameTime);
        if (_accumulator < UpdateInterval)
            return;

        _accumulator = TimeSpan.Zero;

        // Forget AIs that closed the window or no longer exist.
        foreach (var uid in _lastSent.Keys)
        {
            if (!_userInterface.IsUiOpen(uid, CamWarpUiKey.Key))
                _closed.Add(uid);
        }

        foreach (var uid in _closed)
        {
            _lastSent.Remove(uid);
        }

        _closed.Clear();

        var query = EntityQueryEnumerator<StationAiHeldComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (!_userInterface.IsUiOpen((uid, ui), CamWarpUiKey.Key))
                continue;

            if (!_stationAiSystem.TryGetCore(uid, out var core))
                continue;

            var cameras = GetCameras(core.Owner);
            if (_lastSent.TryGetValue(uid, out var last) && last.SequenceEqual(cameras))
                continue;

            SendCameras(uid, cameras);
        }
    }

    private void SendCameras(EntityUid uid, List<CameraWarpData> cameras)
    {
        _lastSent[uid] = cameras;
        // The client sorts the list it is given, so hand it a copy and keep ours for comparing.
        _userInterface.SetUiState(uid, CamWarpUiKey.Key, new CameraWarpBuiState(new List<CameraWarpData>(cameras)));
    }
    // QB edit end

    private List<CameraWarpData> GetCameras(EntityUid coreUid)
    {
        List<CameraWarpData> cameras = new();

        var query = EntityManager.EntityQueryEnumerator<SurveillanceCameraComponent>();

        var aiGrid = _xformSystem.GetGrid(coreUid);

        // QB edit start - also send where each camera is on the grid, for the camera map
        if (aiGrid == null)
            return cameras;

        var invMatrix = _xformSystem.GetInvWorldMatrix(aiGrid.Value);
        // QB edit end

        while (query.MoveNext(out var queryUid, out var comp))
        {
            if (_xformSystem.GetGrid(queryUid) != aiGrid || !comp.Active)
                continue;

            var local = Vector2.Transform(_xformSystem.GetWorldPosition(queryUid), invMatrix); // QB

            var data = new CameraWarpData
            {
                NetEntityUid = GetNetEntity(queryUid),
                DisplayName = comp.CameraId,
                Coordinates = GetNetCoordinates(new EntityCoordinates(aiGrid.Value, local)), // QB
            };

            cameras.Add(data);
        }

        return cameras;
    }

    private void OnUiWarpAction(Entity<StationAiHeldComponent> ent, ref CameraWarpActionMessage args)
    {
        if (args.WarpAction == null)
            return;

        var target = GetEntity(args.WarpAction.Target);

        // The UI doesn't get refreshed when a camera goes offline or whatever
        // so we gotta check if it can still be jumped to.

        if (!TryComp<SurveillanceCameraComponent>(target, out var camera) || !camera.Active)
            return;

        if (!_stationAiSystem.TryGetCore(ent.Owner, out var core) || core.Comp?.RemoteEntity == null)
            return;

        // The AI shouldn't be able to jump to cams on other stations/shuttles
        if (_xformSystem.GetGrid(core.Owner) != _xformSystem.GetGrid(target))
            return;

        _xformSystem.SetWorldPosition(core.Comp.RemoteEntity.Value, _xformSystem.GetWorldPosition(target));
    }
}