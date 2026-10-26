// Inspector configuration for the metadata-only debug scene; canonical generation settings stay in WORLD.
using Godot;
[GlobalClass]
public partial class PreviewMapMasterConfig : Resource
{
    [ExportGroup("Canonical production inputs")]
    [Export] public WorldGenerationMasterConfig Generation { get; set; }
    [Export] public WorldLiquidMasterConfig AuthoredPond { get; set; }
    [ExportGroup("Preview session")]
    [Export] public int Seed { get; set; } = 213876;
}
