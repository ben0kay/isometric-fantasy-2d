// Editable production WeatherProfile settings extracted from the proven visual sandbox.
using Godot;

[GlobalClass]
public partial class WeatherProfile : Resource
{
    [ExportGroup("Rain")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float RainIntensity { get => _RainIntensity; set { var next = Mathf.Clamp(value,0f,1f); if (_RainIntensity == next) return; _RainIntensity = next; EmitChanged(); } }
    private float _RainIntensity = 0f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float TransitionSeconds { get; set; } = 2f;
    [Export(PropertyHint.Range, "100,4000,100")] public int MaxDrops { get; set; } = 1800;
    [Export(PropertyHint.Range, "100,800,10")] public float FallSpeed { get; set; } = 420f;
    [Export(PropertyHint.Range, "0,250,5")] public float SlantPerStrength { get; set; } = 100f;
    [ExportGroup("Rain visual layers")]
    [Export] public bool LayeredRain { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DropSoftness { get; set; } = 0.40f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DropVariation { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ForegroundRainStrength { get; set; } = 0.45f;
    [ExportGroup("Wet foliage")]
    [Export] public bool WetFoliage { get => _WetFoliage; set { var next = value; if (_WetFoliage == next) return; _WetFoliage = next; EmitChanged(); } }
    private bool _WetFoliage = true;
    [Export(PropertyHint.Range, "0,0.35,0.01")] public float WetFoliageDarkening { get => _WetFoliageDarkening; set { var next = value; if (_WetFoliageDarkening == next) return; _WetFoliageDarkening = next; EmitChanged(); } }
    private float _WetFoliageDarkening = 0.15f;
    [Export(PropertyHint.Range, "0.1,20,0.1")] public float WetnessBuildSeconds { get; set; } = 3f;
    [Export(PropertyHint.Range, "0.1,60,0.1")] public float WetnessDrySeconds { get; set; } = 8f;
    [ExportGroup("Ground splashes")]
    [Export] public bool Splashes { get => _Splashes; set { var next = value; if (_Splashes == next) return; _Splashes = next; EmitChanged(); } }
    private bool _Splashes = true;
    [Export(PropertyHint.Range, "0,200,1")] public float SplashesPerSecond { get; set; } = 95f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float SplashOpacity { get; set; } = 0.72f;
    [Export(PropertyHint.Range, "0.5,2,0.05")] public float SplashSize { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0.15,0.6,0.01")] public float SplashLifetime { get; set; } = 0.38f;
    [ExportGroup("Lightning")]
    [Export] public bool Lightning { get => _Lightning; set { var next = value; if (_Lightning == next) return; _Lightning = next; EmitChanged(); } }
    private bool _Lightning = true;
    [Export(PropertyHint.Range, "2,120,1")] public float LightningIntervalMin { get; set; } = 8f;
    [Export(PropertyHint.Range, "2,180,1")] public float LightningIntervalMax { get; set; } = 18f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float VisibleStrikeChance { get; set; } = 0.04f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float LightningRainThreshold { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float NearbyFlashStrength { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float StrikeFlashStrength { get; set; } = 0.95f;
    [Export] public Color LightningTint { get; set; } = new(0.90f, 0.95f, 1f);
    [ExportGroup("Overcast")]
    [Export] public bool Overcast { get; set; } = true;
    [Export] public Color OvercastTint { get; set; } = new(0.72f, 0.77f, 0.83f);
    [Export(PropertyHint.Range, "0,1,0.01")] public float OvercastShadowReduction { get; set; } = 0.85f;
}
