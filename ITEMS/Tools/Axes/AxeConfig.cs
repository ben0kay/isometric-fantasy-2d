// Tool-specific swing/aim/hit parameters, independent of inventory and input.
using Godot;
[GlobalClass]
public partial class AxeConfig : AttackProfile
{
    // Axes keep their prop interaction alongside faction-filtered actor damage.
    // =========================================================
    public AxeConfig() { HitTools = true; }
    [ExportGroup("Axe presentation")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Vector2 HandAnchor { get; set; } = new(0f, -16f);
    [Export(PropertyHint.Range, "4,40,1")] public float OrbitRadius { get; set; } = 12f;
    [Export(PropertyHint.Range, "1,40,1")] public float AimSpeed { get; set; } = 22f;
}