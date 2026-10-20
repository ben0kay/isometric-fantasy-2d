// Inspector settings shared by the player's movement and replaceable sprite visual.
using Godot;

[GlobalClass]
public partial class PlayerMasterConfig : Resource
{
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "0.1,12,0.1")] public float WalkSpeed { get; set; } = 4f;
    [Export] public float Gravity { get; set; } = 24f;
    [Export] public float FloorSnap { get; set; } = 0.35f;
    [Export] public float MaxSlopeDegrees { get; set; } = 45f;
    [ExportGroup("Visual")]
    [Export(PropertyHint.Range, "0.005,0.1,0.001")] public float SpritePixelSize { get; set; } = 0.025f;
}
