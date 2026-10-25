// Optional biome eligibility/preferences around major water geography; legacy water tests retain their original interpretation.
using Godot;
[GlobalClass]
public partial class BiomeHydrologyProfile : Resource
{
    [ExportGroup("Freshwater permissions")]
    [Export] public bool AllowFreshwaterLakes { get; set; } = true;
    [Export] public bool AllowFreshwaterRivers { get; set; } = true;
    [ExportGroup("Suitability - geography preference")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float LakeSuitability { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RiverSuitability { get; set; } = 1f;
}
