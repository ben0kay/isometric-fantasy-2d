// One editable overworld biome: shared ground appearance and ordered scenery populations.
using Godot;

[GlobalClass]
public partial class WorldBiomeDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [ExportGroup("Macro generation")]
    [Export] public bool EnabledForGeneration { get; set; } = true;
    [Export] public float GenerationWeight { get; set; } = 1f;
    [Export] public float MinimumRegionDiameter { get; set; } = 4000f;
    [Export] public float MaximumRegionDiameter { get; set; } = 8000f;
    [Export] public Color OverviewColour { get; set; } = new(0.3f, 0.6f, 0.3f);
    [ExportGroup("Local content")]
    [Export] public BiomeEnvironmentProfile Environment { get; set; }
    [ExportGroup("Ground stack")]
    [Export] public BiomeGroundStack GroundStack { get; set; }
    [ExportGroup("Legacy surface - fallback only when no stack")]
    [Export] public GroundSurfaceProfile Surface
    {
        get => GroundStack != null ? GroundStack.ResolveBaseSurface() : _legacySurface;
        set => _legacySurface = value;
    }
    private GroundSurfaceProfile _legacySurface;

    // Per-biome modulation belongs to the layer, never to the shared source profile.
    // =========================================================
    public Color GroundTint => GroundStack?.DefiningBaseGround?.Tint ?? Colors.White;
    [Export] public Godot.Collections.Array<BiomePopulation> Populations { get; set; } = new();
}
