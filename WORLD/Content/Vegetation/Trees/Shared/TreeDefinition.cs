// Canonical Resource-only species data; all production trees use Shared/Tree.tscn.
// Inherited scene Variants are retained for scenery compatibility but unused by trees.
using Godot;
using FantasyGame.WorldInteractions.Fire;

[GlobalClass]
public partial class TreeDefinition : SceneryDefinition
{
    [ExportGroup("Tree artwork variants")]
    [Export] public Godot.Collections.Array<TreeVisualVariant> VisualVariants { get; set; } = new();
    [ExportGroup("Foliage shared by ambient and hit leaves")]
    [Export] public bool HasFoliage { get; set; } = true;
    [Export] public Color FoliageColour { get; set; } = new(0.58f, 0.57f, 0.24f);
    [ExportGroup("Visibility feedback")]
    [Export] public OcclusionProfile VisibilityProfile { get; set; } = new();
    [ExportGroup("Shared tree responses")]
    [Export] public bool HitResponseEnabled { get; set; } = true;
    [Export] public TreeHitConfig HitConfig { get; set; } = new();
    [Export] public ShaderMaterial WindMaterial { get; set; }
    [Export] public bool CastLocalLightShadows { get; set; } = true;
    [Export] public int SnowFoliageProfile { get; set; }
    [Export] public bool CanBurn { get; set; } = true;
    [Export] public TreeFireProfile FireProfile { get; set; } = new();
}
