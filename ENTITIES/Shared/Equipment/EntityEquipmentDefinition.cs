// Enemy equipment presentation data; no inventory identity, damage, or player-item dependency.
using Godot;
[GlobalClass]
public partial class EntityEquipmentDefinition : Resource
{
    [Export] public Texture2D Sprite { get; set; }
    [Export] public Vector2 SpriteScale { get; set; } = Vector2.One;
    [Export] public Vector2 GripOffset { get; set; }
    [Export] public Vector2 HandOffset { get; set; } = new(0,-16);
    [Export(PropertyHint.Range,"0,24,1")] public float HoldDistance { get; set; } = 10;
}
