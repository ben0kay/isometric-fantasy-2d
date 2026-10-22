// One editable overworld biome: shared ground appearance and ordered scenery populations.
using Godot;

[GlobalClass]
public partial class WorldBiomeDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public BiomeEnvironmentProfile Environment { get; set; }
    [Export] public GroundSurfaceProfile Surface { get; set; }
    [Export] public Godot.Collections.Array<BiomePopulation> Populations { get; set; } = new();
}
