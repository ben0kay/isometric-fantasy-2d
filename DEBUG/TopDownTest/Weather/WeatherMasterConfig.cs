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
    [ExportGroup("Overcast")]
    [Export] public bool Overcast { get; set; } = true;
    [Export] public Color OvercastTint { get; set; } = new(0.72f, 0.77f, 0.83f);
}
