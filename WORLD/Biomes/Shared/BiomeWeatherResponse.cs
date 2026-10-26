// Optional local presentation response; null is neutral and does not reinterpret the biome climate baseline.
using Godot;
using System;
[GlobalClass]
public partial class BiomeWeatherResponse : Resource
{
    [Export] public bool AllowSnow { get; set; } = true;
    [Export] public bool AllowRain { get; set; } = true;
    [Export] public float TemperatureOffsetCelsius { get; set; }
    [Export] public float SeasonalTemperatureScale { get; set; } = 1;
    [Export] public float SnowCoverMultiplier { get; set; } = 1;
    // Shared response validation runs once; colour palettes retain their existing species owners.
    // =========================================================
    public void Validate()
    {
        if(!float.IsFinite(TemperatureOffsetCelsius) || Mathf.Abs(TemperatureOffsetCelsius)>60
            || !float.IsFinite(SeasonalTemperatureScale) || SeasonalTemperatureScale<0 || SeasonalTemperatureScale>3
            || !float.IsFinite(SnowCoverMultiplier) || SnowCoverMultiplier<0 || SnowCoverMultiplier>1)
            throw new ArgumentException("Invalid biome weather response.");
    }
}
