// Main-hand authored identity, two independent attacks and reusable sprite grip; never mutable inventory state.
using Godot;
[GlobalClass]
public partial class EquippedItemDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public AttackProfile PrimaryAttack { get; set; }
    [Export] public AttackProfile SecondaryAttack { get; set; }
    [ExportGroup("Optional harvesting capability")]
    [Export] public string HarvestTool { get; set; } = "";
    [Export] public float PrimaryHarvestWork { get; set; }
    [Export] public float SecondaryHarvestWork { get; set; }
    [ExportGroup("Held artwork")]
    [Export] public Texture2D Texture { get; set; }
    [Export] public bool Centered { get; set; }
    [Export] public Vector2 GripOffset { get; set; }
    [Export] public Vector2 SpriteScale { get; set; } = Vector2.One;
    [Export] public float SpriteRotationDegrees { get; set; }
}
