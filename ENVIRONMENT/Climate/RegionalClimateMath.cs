// Pure seasonal climate resolution shared by runtime, metadata previews and focused tests.
using Godot;
public static class RegionalClimateMath
{
    // January is the coldest phase; July is warmest. Intrinsic biome baselines are not added twice.
    // =========================================================
    public static RegionalConditions Resolve(WorldClimateSample baseline,float yearPosition,RegionalClimateMasterConfig config,BiomeWeatherResponse response=null)
    {
        float amplitude=config.SeasonalAmplitudeCelsius*(response?.SeasonalTemperatureScale ?? 1);
        float offset=response?.TemperatureOffsetCelsius ?? 0;
        float temperature=baseline.TemperatureCelsius+offset-amplitude*Mathf.Cos(yearPosition*Mathf.Tau);
        float snow=1-Mathf.SmoothStep(-config.SnowTransitionCelsius,config.SnowTransitionCelsius,temperature);
        bool snowAllowed=response?.AllowSnow ?? true;
        float permanent=snowAllowed && baseline.TemperatureCelsius+offset+amplitude<0 ? 1 : 0;
        return new(temperature,(response?.AllowRain ?? true) ? 1-snow : 0,snowAllowed ? snow : 0,permanent,snowAllowed ? response?.SnowCoverMultiplier ?? 1 : 0);
    }
}
