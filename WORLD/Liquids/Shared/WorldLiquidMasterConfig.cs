// Disposable liquid preview settings; geometry is built once at startup, appearance stays live.
using Godot;

[GlobalClass]
public partial class WorldLiquidMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool Animate { get; set; } = true;
    [Export] public bool Shoreline { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RippleStrength { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,28,1")] public float ShoreWidth { get; set; } = 18f;
    [ExportGroup("Rain surface splashes")]
    [Export] public bool RainSurfaceSplashes { get; set; } = true;
    [Export(PropertyHint.Range, "0,180,5")] public float RainSplashRate { get; set; } = 72f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float RainSplashOpacity { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0.5,2,0.05")] public float RainSplashSize { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.2,1.2,0.05")] public float RainSplashLifetime { get; set; } = 0.60f;
    [ExportGroup("Startup shape")]
    [Export] public Vector2 Centre { get; set; } = new(1080f, 800f);
    [Export] public Vector2 Radius { get; set; } = new(270f, 170f);
    [Export] public int ShapeSeed { get; set; } = 6319;
}
