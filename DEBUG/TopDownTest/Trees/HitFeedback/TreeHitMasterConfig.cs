// Tree-specific impact presentation, independent of weapon reach, input and future health.
using Godot;

[GlobalClass]
public partial class TreeHitMasterConfig : Resource
{
    [Export] public float WobbleSeconds { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,6,0.1")] public float WobbleDegrees { get; set; } = 2.8f;
    [Export(PropertyHint.Range, "0,24,1")] public int LeavesPerHit { get; set; } = 10;
    [Export] public Color LeafColour { get; set; } = new(0.58f, 0.57f, 0.24f);
}
