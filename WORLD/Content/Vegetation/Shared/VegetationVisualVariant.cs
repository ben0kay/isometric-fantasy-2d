// Inline artwork data for one cosmetic variant; Texture2D also accepts AtlasTexture regions.
using Godot;

[GlobalClass]
public partial class VegetationVisualVariant : Resource
{
    [Export] public Texture2D Texture { get; set; }
    [ExportGroup("Rooted artwork")]
    [Export] public Vector2 SourceAnchor { get; set; }
    [Export] public Vector2 GroundOffset { get; set; }
    [Export] public Vector2 SpriteScale { get; set; } = Vector2.One;
    [ExportGroup("Conservative root-local bounds, before population scale")]
    [Export] public Vector2 VisualMinimum { get; set; } = new(-55, -100);
    [Export] public Vector2 VisualMaximum { get; set; } = new(55, 0);
    [Export] public float SpacingRadius { get; set; } = -1f;

    // A negative override inherits the definition spacing; bounds already include the artwork transform.
    // =========================================================
    public float PlacementRadius(float definitionRadius) => Mathf.Max(0f, SpacingRadius < 0 ? definitionRadius : SpacingRadius);
}
