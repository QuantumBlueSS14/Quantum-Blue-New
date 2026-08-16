using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Audio;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem), typeof(CrewMonitoringServerSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    /// <summary>
    /// Whether this monitor should emit crit/dead alert beeps.
    /// </summary>
    [DataField("alertsEnabled")]
    public bool AlertsEnabled = true;

    /// <summary>
    /// Shared alert sound used by monitor transitions and scripted pulses.
    /// </summary>
    [DataField("alertSound")]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/_DV/Medical/CrewMonitoring/crew_alert.ogg");

    /// <summary>
    /// Minimum delay between monitor beeps for this console.
    /// </summary>
    [DataField("alertCooldown")]
    public TimeSpan AlertCooldown = TimeSpan.FromSeconds(0.8);

    /// <summary>
    /// Next time this console is allowed to play an alert.
    /// </summary>
    [ViewVariables]
    [Access(typeof(CrewMonitoringConsoleSystem), typeof(CrewMonitoringServerSystem))]
    public TimeSpan NextAlertAt = TimeSpan.Zero;
}
