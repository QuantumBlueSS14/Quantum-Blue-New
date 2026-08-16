using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Audio.Systems; // QB add

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!; // QB add

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetServerDisconnectedEvent>(OnDisconnected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var servers = EntityQueryEnumerator<CrewMonitoringServerComponent>();

        while (servers.MoveNext(out var id, out var server))
        {
            if (!_singletonServerSystem.IsActiveServer(id))
                continue;

            UpdateTimeout(id);
            BroadcastSensorStatus(id, server);
        }
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        // QB Comment out start
        // sensorStatus.Timestamp = _gameTiming.CurTime;
        // component.SensorStatus[sensorStatus.Address] = sensorStatus;
        // QB Comment out end
        // QB- Keep all status updates going through one path so alert behavior remains consistent.
        SetSensorStatusByAddress(uid, args.SenderAddress, sensorStatus, component);
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
    }

    /// <summary>
    /// Drop the sensor status if it hasn't been updated for to long
    /// </summary>
    private void UpdateTimeout(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        foreach (var (address, sensor) in component.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif.Seconds > component.SensorTimeout)
                component.SensorStatus.Remove(address);
        }
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent? serverComponent = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref serverComponent, ref device))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = serverComponent.SensorStatus
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }

    /// <summary>
    /// Clears sensor data on disconnect
    /// </summary>
    private void OnDisconnected(EntityUid uid, CrewMonitoringServerComponent component, ref DeviceNetServerDisconnectedEvent _)
    {
        component.SensorStatus.Clear();
    }

    public void RemoveSensorStatusByAddress(EntityUid uid, string address, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.SensorStatus.Remove(address);
    }

    public void SetSensorStatusByAddress(EntityUid uid, string address, SuitSensorStatus status, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        //QB Start
        // Beep only on alert escalations (none->crit/dead or crit->dead) to avoid packet spam.
        var hadPrevious = component.SensorStatus.TryGetValue(address, out var previousStatus);
        var oldState = hadPrevious ? GetAlertState(previousStatus!) : MonitorAlertState.None;
        var newState = GetAlertState(status);

        if (newState > oldState)
            PlayConfiguredAlertBeep(uid, component);
        //QB End

        status.Timestamp = _gameTiming.CurTime;
        component.SensorStatus[address] = status;
    }

    /// <summary>
    /// Plays crew monitor alert beeps using each console's configured alert settings.
    /// </summary>
    public void PlayConfiguredAlertBeep(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var consoles = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();
        while (consoles.MoveNext(out var consoleUid, out var console))
        {
            if (!console.AlertsEnabled)
                continue;

            if (_gameTiming.CurTime < console.NextAlertAt)
                continue;

            console.NextAlertAt = _gameTiming.CurTime + console.AlertCooldown;
            _audio.PlayPvs(console.AlertSound, consoleUid);
        }
    }

    /// <summary>
    /// QB new - Maps sensor status to monitor alert state using crew-monitor icon semantics.
    /// </summary>
    private static MonitorAlertState GetAlertState(SuitSensorStatus status)
    {
        if (!status.IsAlive)
            return MonitorAlertState.Dead;

        if (status.DamagePercentage is { } damagePct && damagePct >= 1f)
            return MonitorAlertState.Critical;

        return MonitorAlertState.None;
    }

    //QB new - Monitor alert state for crew-monitor icon semantics.
    private enum MonitorAlertState
    {
        None = 0,
        Critical = 1,
        Dead = 2,
    }
}
