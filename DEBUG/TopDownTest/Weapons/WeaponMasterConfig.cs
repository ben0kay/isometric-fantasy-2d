// Tweakable equipped-sword presentation for the disposable cursor/attack test.
using Godot;

[GlobalClass]
public partial class WeaponMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Vector2 HandAnchor { get; set; } = new(0f, -16f);
    [Export(PropertyHint.Range, "4,40,1")] public float OrbitRadius { get; set; } = 12f;
    [Export(PropertyHint.Range, "0.1,1,0.01")] public float AttackSeconds { get; set; } = 0.32f;
    [Export(PropertyHint.Range, "1,40,1")] public float AimSpeed { get; set; } = 22f;
}
