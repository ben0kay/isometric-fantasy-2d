// Tweakable settings exclusively for the disposable rolling-terrain visual test.
using Godot;

[GlobalClass]
public partial class NaturalTerrainConfig : Resource
{
    [ExportGroup("Ground")]
    [Export(PropertyHint.Range, "30,180,2")] public int WorldWidth { get; set; } = 90;
    [Export(PropertyHint.Range, "30,180,2")] public int WorldDepth { get; set; } = 90;
    [Export(PropertyHint.Range, "1,4,0.5")] public float GridStep { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0,8,0.1")] public float HillHeight { get; set; } = 3.8f;
    [Export(PropertyHint.Range, "0,8,0.1")] public float NoiseHeight { get; set; } = 1.35f;
    [Export] public int TerrainSeed { get; set; } = 4137;
    [ExportGroup("Forest")]
    [Export(PropertyHint.Range, "0,250,1")] public int TreeCount { get; set; } = 70;
    [Export(PropertyHint.Range, "2,10,0.1")] public float TreeHeight { get; set; } = 4.8f;
    [Export(PropertyHint.Range, "0.5,4,0.1")] public float MinimumTreeSpacing { get; set; } = 2.4f;
    [Export] public int TreeSeed { get; set; } = 9372;
    [ExportGroup("Playtest")]
    [Export(PropertyHint.Range, "1,15,0.5")] public float PlayerWalkSpeed { get; set; } = 7.5f;
    [ExportGroup("Sun")]
    [Export(PropertyHint.Range, "0,360,1")] public float SunYaw { get; set; } = -50f;
    [Export(PropertyHint.Range, "-85,-15,1")] public float SunPitch { get; set; } = -53f;
    [Export(PropertyHint.Range, "0,3,0.05")] public float SunEnergy { get; set; } = 1.45f;
}
