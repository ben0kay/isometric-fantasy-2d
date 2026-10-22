// Disposable liquid preview settings; geometry is built once at startup, appearance stays live.
using Godot;

[GlobalClass]
public partial class LiquidMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool Animate { get; set; } = true;
    [Export] public bool Shoreline { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RippleStrength { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,28,1")] public float ShoreWidth { get; set; } = 18f;
    [ExportGroup("Startup shape")]
    [Export] public Vector2 Centre { get; set; } = new(1080f, 800f);
    [Export] public Vector2 Radius { get; set; } = new(270f, 170f);
    [Export] public int ShapeSeed { get; set; } = 6319;
}
