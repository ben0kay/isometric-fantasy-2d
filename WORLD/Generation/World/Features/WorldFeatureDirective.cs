// Authorable request for a world-space reservation, not a biome, asset instance or terrain renderer.
using Godot;

[System.Flags]
public enum WorldPlacementKind { None = 0, LowVegetation = 1, SolidScenery = 2, Path = 4, Water = 8, PointOfInterest = 16, CaveEntrance = 32, MajorTerrain = 64, All = 127 }
public enum WorldFeaturePlacement { AnywhereInZone, FixedAnchor }

[GlobalClass]
public partial class WorldFeatureDirective : Resource
{
    [ExportGroup("Identity and importance")]
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool Required { get; set; } = true;
    [Export] public int Priority { get; set; } = 100;
    [Export] public bool IsStartingArea { get; set; }
    [ExportGroup("Normalized placement (+Y down)")]
    [Export] public WorldFeaturePlacement Placement { get; set; }
    [Export] public Rect2 NormalizedZone { get; set; } = new(0, 0, 1, 1);
    [Export] public Vector2 NormalizedAnchor { get; set; } = new(0.5f, 0.5f);
    [ExportGroup("Footprint (world units)")]
    [Export] public Vector2 Size { get; set; } = new(1000, 1000);
    [Export] public float MinimumScale { get; set; } = 1f;
    [Export] public float MaximumScale { get; set; } = 1f;
    [Export] public Vector2[] NormalizedPolygon { get; set; } = new[] { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, 0.5f) };
    [Export] public float Clearance { get; set; }
    [ExportGroup("Interactions")]
    [Export] public WorldPlacementKind Kind { get; set; } = WorldPlacementKind.PointOfInterest;
    [Export] public WorldPlacementKind AllowedPlacements { get; set; } = WorldPlacementKind.LowVegetation;
    [Export] public Color OverviewColour { get; set; } = new(1, 0.75f, 0.15f);
}
