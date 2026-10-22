// Canonical scenery identity and reusable scene variants; biome populations reference this data.
using Godot;

[GlobalClass]
public partial class SceneryDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Godot.Collections.Array<PackedScene> Variants { get; set; } = new();
    [ExportGroup("Footprint and artwork")]
    [Export] public float SpacingRadius { get; set; } = 30f;
    [Export] public Vector2 VisualMinimum { get; set; } = new(-80, -150);
    [Export] public Vector2 VisualMaximum { get; set; } = new(80, 10);
    [Export] public NodePath SpritePath { get; set; } = "Sprite";
    [Export] public NodePath FootprintPath { get; set; } = "";
}
