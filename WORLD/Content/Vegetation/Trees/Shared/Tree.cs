// One internal template/controller; callers configure species/variant before adding to Actors.
using Godot;
namespace FantasyGame.World.Trees;

public partial class Tree : StaticBody2D
{
    [Export] public TreeDefinition Definition { get; set; }
    [Export] public int VariantIndex { get; set; }
    [Export] public bool Mirrored { get; set; }
    public TreeVisualVariant SelectedVariant { get; private set; }
    private bool _configured;

    #region Configuration before child readiness
    // Generation supplies an already-seeded index; this controller never chooses random content.
    // =========================================================
    public void Configure(TreeDefinition definition, int variantIndex, bool mirrored)
    {
        if (IsInsideTree()) { GD.PushError("Configure trees before adding them to the world."); return; }
        Definition = definition; VariantIndex = variantIndex; Mirrored = mirrored;
        ApplyDefinition();
    }

    // Also supports an authored instance of the internal template with exported Resource data.
    // Parent EnterTree runs before child Ready, so all hit/fire caches see final artwork.
    // =========================================================
    public override void _EnterTree()
    {
        if (!_configured) ApplyDefinition();
        AddToGroup("world_trees");
        SetProcess(false); SetPhysicsProcess(false);
    }

    // Keep stable child paths while configuring every capability from Resource data.
    // =========================================================
    private void ApplyDefinition()
    {
        if (Definition == null || VariantIndex < 0 || VariantIndex >= Definition.VisualVariants.Count
            || Definition.VisualVariants[VariantIndex]?.Texture == null)
            throw new System.InvalidOperationException("Tree requires a valid species and visual variant.");
        SelectedVariant = Definition.VisualVariants[VariantIndex];
        Sprite2D sprite = GetNode<Sprite2D>("Sprite");
        sprite.Texture = SelectedVariant.Texture; sprite.Centered = SelectedVariant.Centered;
        sprite.Offset = SelectedVariant.SpriteOffset; sprite.Scale = SelectedVariant.SpriteScale;
        sprite.FlipH = Mirrored; sprite.Material = SelectedVariant.WindMaterial ?? Definition.WindMaterial;
        Vector2 anchor = SelectedVariant.SourceAnchor;
        if (Mirrored) anchor.X = sprite.Texture.GetWidth() - anchor.X;
        Vector2 imageOrigin = sprite.Centered ? sprite.Texture.GetSize() * -0.5f : Vector2.Zero;
        sprite.Position = SelectedVariant.GroundOffset - (anchor + imageOrigin + sprite.Offset) * sprite.Scale;
        sprite.SetMeta("snow_foliage_profile", Definition.SnowFoliageProfile);
        Vector2[] polygon = (Vector2[])SelectedVariant.TrunkPolygon.Clone();
        if (Mirrored)
        {
            System.Array.Reverse(polygon);
            for (int i = 0; i < polygon.Length; i++) polygon[i].X = -polygon[i].X;
        }
        var footprint = GetNode<CollisionPolygon2D>("TrunkFootprint");
        footprint.Polygon = polygon; footprint.Disabled = polygon.Length < 3;
        var localShadow = GetNode<LocalLightOccluder>("LocalLightOccluder");
        localShadow.Visible = Definition.CastLocalLightShadows;
        Vector2[] lightPolygon = (Vector2[])SelectedVariant.LocalLightPolygon.Clone();
        if (Mirrored)
        {
            System.Array.Reverse(lightPolygon);
            for (int i = 0; i < lightPolygon.Length; i++) lightPolygon[i].X = -lightPolygon[i].X;
        }
        localShadow.Outline = lightPolygon;
        var hits = GetNode<TreeHitResponse>("HitResponse");
        hits.Enabled = Definition.HitResponseEnabled; hits.Config = Definition.HitConfig;
        hits.FoliageColour = Definition.FoliageColour; hits.FoliageEnabled = Definition.HasFoliage;
        GetNode<FantasyGame.WorldInteractions.Fire.Burnable>("Burnable").Profile = Definition.CanBurn
            ? SelectedVariant.ResolveFireProfile(Definition.FireProfile) : null;
        SetMeta("world_content_id", Definition.Id); SetMeta("world_tree_variant", SelectedVariant.Id);
        SetMeta("environment_snow", (int)Definition.SnowResponse);
        SetMeta("environment_grass", false); SetMeta("environment_shadows", Definition.CastSunShadow);
        SetMeta("environment_contact", Definition.ContactShadowRadius);
        _configured = true;
    }
    #endregion
}
