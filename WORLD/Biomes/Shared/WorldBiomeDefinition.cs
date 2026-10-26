// One editable overworld biome: shared ground appearance and ordered scenery populations.
using Godot;
using System;

[GlobalClass]
public partial class WorldBiomeDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    // One identity tone feeds the map and base ground; omitted values inherit the surface tone.
    // =========================================================
    [Export] public Color Colour { get => _colour ?? Surface?.BaseColour ?? Colors.White; set => _colour=value; }
    private Color? _colour;
    // Source compatibility for older C# callers; no second Inspector colour.
    public Color OverviewColour { get => Colour; set => Colour=value; }
    [ExportGroup("Macro generation")]
    [Export] public bool EnabledForGeneration { get; set; } = true;
    [Export] public float GenerationWeight { get; set; } = 1f;
    [Export] public float MinimumRegionDiameter { get; set; } = 4000f;
    [Export] public float MaximumRegionDiameter { get; set; } = 8000f;
    [ExportGroup("Optional macro climate placement - BC2")]
    [Export] public BiomePlacementClimate PlacementClimate { get; set; }
    [ExportGroup("Hydrology")]
    [Export] public BiomeHydrologyProfile Hydrology { get; set; }
    [ExportGroup("Local content")]
    [Export] public BiomeEnvironmentProfile Environment { get; set; }
    [ExportGroup("Climate baseline - configuration only")]
    [Export] public BiomeClimateProfile Climate { get; set; }
    [ExportGroup("Future spawning - configuration only")]
    [Export] public BiomeEntitySpawnProfile EntitySpawns { get; set; }
    [Export] public BiomeResourceSpawnProfile ResourceSpawns { get; set; }
    [ExportGroup("Ground stack")]
    [Export] public BiomeGroundStack GroundStack { get; set; }
    [ExportGroup("Legacy surface - fallback only when no stack")]
    [Export] public GroundSurfaceProfile Surface
    {
        get => GroundStack != null ? GroundStack.ResolveBaseSurface() : _legacySurface;
        set => _legacySurface = value;
    }
    private GroundSurfaceProfile _legacySurface;

    // Convert the chosen tone to modulation once during setup; preserve artwork and patch detail.
    // =========================================================
    public Color GroundTint
    {
        get
        {
            ValidateColour();
            var source=Surface?.BaseColour ?? Colors.White; var tone=Colour;
            float Channel(float target,float original) => target==original ? 1f : target/Mathf.Max(original,0.001f);
            var tint=new Color(Channel(tone.R,source.R),Channel(tone.G,source.G),Channel(tone.B,source.B),1f);
            return tint*(GroundStack?.DefiningBaseGround?.Tint ?? Colors.White);
        }
    }
    // Reject invalid identity data before map/texture creation.
    // =========================================================
    public void ValidateColour()
    {
        var colour=Colour;
        if(!float.IsFinite(colour.R) || !float.IsFinite(colour.G) || !float.IsFinite(colour.B)
            || colour.R<0 || colour.R>1 || colour.G<0 || colour.G>1 || colour.B<0 || colour.B>1 || colour.A!=1f)
            throw new ArgumentException("Biome identity colour must be finite, opaque and within 0–1.");
    }
    [Export] public Godot.Collections.Array<BiomePopulation> Populations { get; set; } = new();
}
