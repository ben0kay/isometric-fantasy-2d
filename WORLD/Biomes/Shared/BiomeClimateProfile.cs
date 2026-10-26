// Authored climate baselines only; no weather simulation or per-region runtime state.
using Godot;
using System;

[GlobalClass]
public partial class BiomeClimateProfile : Resource
{
    public const float DefaultTemperatureCelsius = 15f;
    public const float DefaultRelativeHumidityPercent = 65f;
    [ExportGroup("Baseline climate - not current weather")]
    [Export(PropertyHint.Range, "-100,100,0.1")] public float BaselineTemperatureCelsius { get; set; } = DefaultTemperatureCelsius;
    [Export(PropertyHint.Range, "0,100,0.1")] public float BaselineRelativeHumidityPercent { get; set; } = DefaultRelativeHumidityPercent;

    // Explicit authoring validation; Inspector hints alone do not reject invalid serialized values.
    // =========================================================
    public void Validate()
    {
        if (!float.IsFinite(BaselineTemperatureCelsius) || BaselineTemperatureCelsius < -100f || BaselineTemperatureCelsius > 100f
            || !float.IsFinite(BaselineRelativeHumidityPercent) || BaselineRelativeHumidityPercent < 0f || BaselineRelativeHumidityPercent > 100f)
            throw new ArgumentException("Biome climate requires -100..100 degrees Celsius and 0..100 percent relative humidity.");
    }
}
