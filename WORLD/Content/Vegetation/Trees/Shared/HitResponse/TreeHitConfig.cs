// Optional embedded wobble/burst tuning; foliage colour belongs to TreeDefinition.
using Godot;
[GlobalClass]
public partial class TreeHitConfig : Resource
{
    [Export] public float WobbleSeconds { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,6,0.1")] public float WobbleDegrees { get; set; } = 2.8f;
    [Export(PropertyHint.Range, "0,24,1")] public int LeavesPerHit { get; set; } = 10;
}