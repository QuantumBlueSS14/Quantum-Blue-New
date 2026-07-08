using System;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.QB.LuminanceCalculator;

public enum LuminanceComparison
{
    LessThan,
    LessThanOrEqual,
    Equal,
    GreaterThanOrEqual,
    GreaterThan,
}

public sealed class LuminanceAtCoordinateSystem : EntitySystem
{
    private const float EqualEpsilon = 0.0001f;

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private bool _clampToPvs;
    private float _standardPvsRange;
    private float _priorityPvsRange;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CVars.NetPVS, value => _clampToPvs = value, true);
        _cfg.OnValueChanged(CVars.NetMaxUpdateRange, value => RecalculatePvsRanges(value, null), true);
        _cfg.OnValueChanged(CVars.NetPvsPriorityRange, value => RecalculatePvsRanges(null, value), true);
    }

    private void RecalculatePvsRanges(float? standardPvsRange, float? priorityPvsRange)
    {
        if (standardPvsRange.HasValue)
            _standardPvsRange = standardPvsRange.Value / 2f;

        if (priorityPvsRange.HasValue)
            _priorityPvsRange = priorityPvsRange.Value / 2f;

        _priorityPvsRange = Math.Max(_standardPvsRange, _priorityPvsRange);
    }

    public LuminanceCheckResult Evaluate(
        MapCoordinates mapCoords,
        float ambientThreshold,
        LuminanceComparison comparison = LuminanceComparison.LessThanOrEqual)
    {
        var pointLuminance = 0f;

        var lights = EntityQueryEnumerator<PointLightComponent, TransformComponent>();
        while (lights.MoveNext(out _, out var light, out var lightXform))
        {
            TryAccumulateLightContribution(
                mapCoords,
                light,
                lightXform,
                _clampToPvs,
                _standardPvsRange,
                _priorityPvsRange,
                ref pointLuminance);
        }

        return new LuminanceCheckResult(
            CompareLuminance(pointLuminance, ambientThreshold, comparison),
            pointLuminance,
            ambientThreshold);
    }

    private static bool CompareLuminance(
        float pointLuminance,
        float ambientThreshold,
        LuminanceComparison comparison)
    {
        return comparison switch
        {
            LuminanceComparison.LessThan => pointLuminance < ambientThreshold,
            LuminanceComparison.LessThanOrEqual => pointLuminance <= ambientThreshold,
            LuminanceComparison.Equal => MathF.Abs(pointLuminance - ambientThreshold) <= EqualEpsilon,
            LuminanceComparison.GreaterThanOrEqual => pointLuminance >= ambientThreshold,
            LuminanceComparison.GreaterThan => pointLuminance > ambientThreshold,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unhandled luminance comparison value."),
        };
    }

    private void TryAccumulateLightContribution(
        MapCoordinates mapCoords,
        PointLightComponent light,
        TransformComponent lightXform,
        bool clampToPvs,
        float standardPvsRange,
        float priorityPvsRange,
        ref float pointLuminance)
    {
        if (!light.Enabled || light.Energy <= 0f || lightXform.MapID != mapCoords.MapId)
            return;

        var lightPos = _xform.GetWorldPosition(lightXform);
        var delta = lightPos - mapCoords.Position;
        var dist = delta.Length();
        if (light.Radius <= 0f || dist >= light.Radius)
            return;

        if (!IsLightInPvsRange(delta, light, clampToPvs, standardPvsRange, priorityPvsRange))
            return;

        var lightMapCoords = new MapCoordinates(lightPos, mapCoords.MapId);
        if (!_examine.InRangeUnOccluded(mapCoords, lightMapCoords, dist + 0.01f, null))
            return;

        var normalizedDist = dist / light.Radius;
        var attenuation = 1f - normalizedDist;
        if (attenuation <= 0f)
            return;

        var lightLuminance = (light.Color.R * 0.2126f)
            + (light.Color.G * 0.7152f)
            + (light.Color.B * 0.0722f);
        pointLuminance += lightLuminance * light.Energy * attenuation;
    }

    private static bool IsLightInPvsRange(
        System.Numerics.Vector2 delta,
        PointLightComponent light,
        bool clampToPvs,
        float standardPvsRange,
        float priorityPvsRange)
    {
        if (!clampToPvs)
            return true;

        var pvsRange = IsHighPriorityLight(light)
            ? priorityPvsRange
            : standardPvsRange;

        return MathF.Abs(delta.X) <= pvsRange
            && MathF.Abs(delta.Y) <= pvsRange;
    }

    private static bool IsHighPriorityLight(PointLightComponent light)
    {
        return light is { Enabled: true, CastShadows: true, Radius: > 7, LifeStage: <= ComponentLifeStage.Running };
    }
}

public readonly record struct LuminanceCheckResult(
    bool MeetsThreshold,
    float PointLuminance,
    float AmbientThreshold);
