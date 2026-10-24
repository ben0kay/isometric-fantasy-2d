// Optional biome permissions for shared water planning; null uses permissive compatibility defaults.
using Godot;
[GlobalClass]
public partial class BiomeHydrologyProfile : Resource
{
    [ExportGroup("Freshwater permissions")]
    [Export] public bool AllowFreshwaterLakes { get; set; } = true;
    [Export] public bool AllowFreshwaterRivers { get; set; } = true;
    [ExportGroup("Suitability - probability, not coverage")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float LakeSuitability { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RiverSuitability { get; set; } = 1f;
}
