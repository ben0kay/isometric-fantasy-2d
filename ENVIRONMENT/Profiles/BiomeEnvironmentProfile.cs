// Optional biome overrides: null components inherit world defaults; renderers are always shared.
using Godot;
[GlobalClass]
public partial class BiomeEnvironmentProfile : Resource
{
    [Export] public DayNightProfile Lighting { get; set; }
    [Export] public WindMasterConfig Wind { get; set; }
    [Export] public AtmosphereProfile Atmosphere { get; set; }
    [Export] public WeatherProfile Weather { get; set; }
    [Export] public SnowProfile Snow { get; set; }
    [Export] public ColourGradeProfile ColourGrade { get; set; }
    [Export] public CloudShadeProfile Clouds { get; set; }
    [Export] public TorchProfile Torch { get; set; }

    // Resolve per-component overrides and clone data so live tools never mutate authored resources.
    // =========================================================
    public BiomeEnvironmentProfile Resolve(BiomeEnvironmentProfile defaults) => new BiomeEnvironmentProfile
    {
        Lighting = (DayNightProfile)(Lighting ?? defaults.Lighting).Duplicate(true),
        Wind = (WindMasterConfig)(Wind ?? defaults.Wind).Duplicate(true),
        Atmosphere = (AtmosphereProfile)(Atmosphere ?? defaults.Atmosphere).Duplicate(true),
        Weather = (WeatherProfile)(Weather ?? defaults.Weather).Duplicate(true),
        Snow = (SnowProfile)(Snow ?? defaults.Snow).Duplicate(true),
        ColourGrade = (ColourGradeProfile)(ColourGrade ?? defaults.ColourGrade).Duplicate(true),
        Clouds = (CloudShadeProfile)(Clouds ?? defaults.Clouds).Duplicate(true),
        Torch = (TorchProfile)(Torch ?? defaults.Torch).Duplicate(true)
    };
}
