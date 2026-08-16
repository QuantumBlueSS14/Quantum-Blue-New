using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CrewMonitoringConsoleSystem))]
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
    /// Track sensors that have already triggered a crit/dead alert on this console.
    /// </summary>
    public HashSet<string> AlertedSensors = [];

    /// <summary>
    /// Shared alert sound used by monitor transitions and scripted pulses.
    /// </summary>
    [DataField("alertSound")]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/_DV/Medical/CrewMonitoring/crew_alert.ogg");

    /// <summary>
    /// Minimum delay between monitor beeps for this console.
    /// </summary>
    [DataField("alertCooldown")]
    public TimeSpan AlertCooldown = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Next time this console is allowed to play an alert.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextAlert;
}
