// Shared directional guard settings; independent from shield inventory, player input and actor movement.
using Godot;
[GlobalClass]
public partial class GuardProfile : Resource
{
    [Export(PropertyHint.Range,"0,1,0.05")] public float Reduction { get; set; } = 0.6f;
    [Export(PropertyHint.Range,"1,89,1")] public float HalfAngleDegrees { get; set; } = 70;
    [Export(PropertyHint.Range,"0.1,1,0.05")] public float MovementMultiplier { get; set; } = 0.45f;
    [Export] public bool ShowDebugMarker { get; set; } = true;
}
