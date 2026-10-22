// Tool-specific swing/aim/hit parameters, independent of inventory and input.
using Godot;
[GlobalClass]
public partial class AxeConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Vector2 HandAnchor { get; set; } = new(0f, -16f);
    [Export(PropertyHint.Range, "4,40,1")] public float OrbitRadius { get; set; } = 12f;
    [Export(PropertyHint.Range, "0.1,1,0.01")] public float AttackSeconds { get; set; } = 0.38f;
    [Export(PropertyHint.Range, "1,40,1")] public float AimSpeed { get; set; } = 22f;
    [Export(PropertyHint.Range, "8,100,1")] public float HitReach { get; set; } = 52f;
    [Export(PropertyHint.Range, "1,89,1")] public float HitHalfAngleDegrees { get; set; } = 65f;
    [Export(PropertyHint.Range, "0.05,0.95,0.01")] public float ContactProgress { get; set; } = 0.85f;
}