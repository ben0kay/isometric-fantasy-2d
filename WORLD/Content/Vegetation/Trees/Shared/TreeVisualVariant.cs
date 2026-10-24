// Resource-only artwork, anchor and trunk footprint for one species variant.
using Godot;
using System.Collections.Generic;
using FantasyGame.WorldInteractions.Fire;

[GlobalClass]
public partial class TreeVisualVariant : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public Texture2D Texture { get; set; }
    [Export] public Texture2D FoliageMask { get; set; }
    [ExportGroup("Sprite and ground anchor")]
    [Export] public Vector2 SourceAnchor { get; set; }
    [Export] public Vector2 GroundOffset { get; set; }
    [Export] public Vector2 SpriteScale { get; set; } = Vector2.One;
    [Export] public Vector2 SpriteOffset { get; set; }
    [Export] public bool Centered { get; set; }
    [Export] public Vector2[] TrunkPolygon { get; set; } = System.Array.Empty<Vector2>();
    [ExportGroup("Conservative placement bounds, before population scale")]
    [Export] public float SpacingRadius { get; set; } = -1f;
    [Export] public Vector2 VisualMinimum { get; set; } = new(-80, -150);
    [Export] public Vector2 VisualMaximum { get; set; } = new(80, 10);
    [ExportGroup("Optional local-light outline, root-local; empty uses trunk") ]
    [Export] public Vector2[] LocalLightPolygon { get; set; } = System.Array.Empty<Vector2>();
    [ExportGroup("Optional per-artwork overrides")]
    [Export] public ShaderMaterial WindMaterial { get; set; }
    [Export] public TreeFireProfile FireProfile { get; set; }
    private readonly Dictionary<TreeFireProfile, TreeFireProfile> _fireProfiles = new();

    // Never reserve less than the selected trunk's actual footprint, even for broad variants.
    // A negative override inherits the species spacing radius.
    // =========================================================
    public float PlacementRadius(float speciesRadius)
    {
        float radius = Mathf.Max(0f, SpacingRadius < 0f ? speciesRadius : SpacingRadius);
        foreach (Vector2 point in TrunkPolygon) radius = Mathf.Max(radius, point.Length());
        return radius;
    }

    // One resolved profile per variant/source profile, shared by all its instances.
    // Species assets are never mutated to substitute another variant's healthy artwork.
    // =========================================================
    public TreeFireProfile ResolveFireProfile(TreeFireProfile speciesProfile)
    {
        TreeFireProfile source = FireProfile ?? speciesProfile;
        if (source == null) return null;
        if (_fireProfiles.TryGetValue(source, out TreeFireProfile cached)) return cached;
        var resolved = (TreeFireProfile)source.Duplicate();
        resolved.HealthyTexture = Texture; resolved.HealthyAnchor = SourceAnchor;
        _fireProfiles.Add(source, resolved); return resolved;
    }
}
