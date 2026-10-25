// Configure once before publication; no polling, physics, species logic or instance-owned materials.
using Godot;
using System;

public partial class VegetationInstance : Node2D
{
    public VegetationDefinition Definition { get; private set; }
    public int VariantIndex { get; private set; }
    public bool Mirrored { get; private set; }
    public VegetationVisualVariant SelectedVariant { get; private set; }
    private bool _configured;

    // The caller owns position, population scale and stable world-instance metadata.
    // =========================================================
    public void Configure(VegetationDefinition definition, int variantIndex, bool mirrored)
    {
        if (IsInsideTree()) throw new InvalidOperationException("Configure vegetation before adding it to the world.");
        if (definition == null || variantIndex < 0 || variantIndex >= definition.VisualVariants.Count
            || definition.VisualVariants[variantIndex]?.Texture == null)
            throw new ArgumentException("Vegetation requires a valid definition and visual variant.");
        var art = definition.VisualVariants[variantIndex];
        if (!art.SpriteScale.IsFinite() || art.SpriteScale.X <= 0 || art.SpriteScale.Y <= 0
            || !art.SourceAnchor.IsFinite() || !art.GroundOffset.IsFinite())
            throw new ArgumentException("Vegetation artwork requires finite anchors and positive scale.");
        var sprite = GetNode<Sprite2D>("Sprite");
        Definition = definition; VariantIndex = variantIndex; Mirrored = mirrored; SelectedVariant = art;
        sprite.Texture = art.Texture; sprite.Centered = false; sprite.Offset = Vector2.Zero;
        sprite.Scale = art.SpriteScale; sprite.FlipH = mirrored; sprite.Material = definition.WindMaterial;
        Vector2 anchor = art.SourceAnchor;
        if (mirrored) anchor.X = art.Texture.GetWidth() - anchor.X;
        Vector2 ground = art.GroundOffset;
        if (mirrored) ground.X = -ground.X;
        sprite.Position = ground - anchor * art.SpriteScale;
        _configured = true; SetProcess(false); SetPhysicsProcess(false);
    }

    // Unconfigured templates fail clearly before child readiness; no automatic instance randomness.
    // =========================================================
    public override void _EnterTree()
    {
        if (!_configured) throw new InvalidOperationException("Call Configure before publishing vegetation.");
        SetProcess(false); SetPhysicsProcess(false);
    }
}
