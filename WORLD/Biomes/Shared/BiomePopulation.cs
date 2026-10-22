// Inline biome population settings; content identity/art remain in the referenced definition.
using Godot;

[GlobalClass]
public partial class BiomePopulation : Resource
{
    [Export] public SceneryDefinition Content { get; set; }
    [ExportGroup("Preview population")]
    [Export(PropertyHint.Range, "0,5000,1")] public int Count { get; set; } = 1;
    [ExportGroup("Streamed density (per million square world units)")]
    [Export] public float InstancesPerMillionUnits { get; set; } = 10f;
    [ExportGroup("Shared appearance")]
    [Export] public float MinimumScale { get; set; } = 1f;
    [Export] public float MaximumScale { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float MirrorChance { get; set; } = 0.5f;
    [ExportGroup("Preview-only clusters")]
    [Export] public int ClusterSize { get; set; } = 1;
    [Export] public float ClusterRadius { get; set; } = 55f;
}
