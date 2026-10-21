// Disposable rain and shared wind controls; these are not production weather systems.
using Godot;

[GlobalClass]
public partial class WeatherMasterConfig : Resource
{
    [ExportGroup("Shared wind")]
    [Export(PropertyHint.Range, "-3,3,0.05")] public float WindStrength { get; set; } = 1f;
    [Export] public Vector2 DriftPerStrength { get; set; } = new(5f, 1.5f);
    [ExportGroup("Rain")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float RainIntensity { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float TransitionSeconds { get; set; } = 2f;
    [Export(PropertyHint.Range, "100,4000,100")] public int MaxDrops { get; set; } = 1800;
    [Export(PropertyHint.Range, "100,800,10")] public float FallSpeed { get; set; } = 420f;
    [Export(PropertyHint.Range, "0,250,5")] public float SlantPerStrength { get; set; } = 100f;
    [ExportGroup("Ground splashes")]
    [Export] public bool Splashes { get; set; } = true;
    [Export(PropertyHint.Range, "0,200,1")] public float SplashesPerSecond { get; set; } = 95f;
    [ExportGroup("Lightning")]
    [Export] public bool Lightning { get; set; } = true;
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
}
