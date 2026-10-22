// World-wide clock and fallback visual profiles; biome settings can override individual components.
using Godot;
[GlobalClass]
public partial class EnvironmentMasterConfig : Resource
{
    [ExportGroup("Scenery visual scheduling")]
    [Export] public bool CullOffscreenSceneryEffects { get; set; } = true;
    [Export(PropertyHint.Range, "64,512,16")] public float SceneryPrepareMargin { get; set; } = 160f;
    [Export(PropertyHint.Range, "16,256,16")] public float SceneryRetentionMargin { get; set; } = 96f;
    [Export] public ClockMasterConfig Clock { get; set; }
    [Export] public BiomeEnvironmentProfile Defaults { get; set; }
}
