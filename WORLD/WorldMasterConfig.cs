// Production world bounds and generation controls; scenery and palettes belong to biome data.
using Godot;

[GlobalClass]
public partial class WorldMasterConfig : Resource
{
    [ExportGroup("World")]
    [Export] public Rect2 Bounds { get; set; } = new(-800, -544, 2560, 1728);
    [Export] public Vector2 PlayerSpawn { get; set; } = new(480, 320);
    [Export] public WorldBiomeDefinition Biome { get; set; }
    [ExportGroup("Seed")]
    [Export] public bool RandomSeedOnLoad { get; set; } = true;
    [Export] public int FixedSeed { get; set; } = 213876;
    [ExportGroup("Placement")]
    [Export(PropertyHint.Range, "1,120,1")] public int AttemptsPerObject { get; set; } = 60;
    [Export] public float SpawnClearance { get; set; } = 120f;
    [Export] public float EdgeMargin { get; set; } = 20f;
    [Export] public float ExtraSpacing { get; set; } = 6f;
}
