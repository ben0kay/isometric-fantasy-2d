// Tunable distribution of four decorative grass sprites in the 2D debug world.
using Godot;

[GlobalClass]
public partial class GrassPatchMasterConfig : Resource
{
    [ExportGroup("Placement")]
    [Export(PropertyHint.Range, "0,100,1")] public int ClusterCount { get; set; } = 36;
    [Export(PropertyHint.Range, "1,15,1")] public int ClumpsPerClusterMin { get; set; } = 3;
    [Export(PropertyHint.Range, "1,15,1")] public int ClumpsPerClusterMax { get; set; } = 7;
    [Export(PropertyHint.Range, "0,200,1")] public int IsolatedClumps { get; set; } = 30;
    [Export(PropertyHint.Range, "15,120,5")] public float ClusterRadius { get; set; } = 55f;
    [Export(PropertyHint.Range, "0,60,1")] public float MinimumSpacing { get; set; } = 17f;
    [Export(PropertyHint.Range, "0,45,1")] public float ObstacleClearance { get; set; } = 10f;
    [Export(PropertyHint.Range, "0,80,5")] public float EdgeMargin { get; set; } = 30f;
    [Export(PropertyHint.Range, "1,20,1")] public int AttemptsPerClump { get; set; } = 7;

    [ExportGroup("Visual variation")]
    [Export(PropertyHint.Range, "0.1,0.8,0.01")] public float MinimumScale { get; set; } = 0.26f;
    [Export(PropertyHint.Range, "0.1,0.8,0.01")] public float MaximumScale { get; set; } = 0.42f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float MirrorChance { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0.5,1.2,0.01")] public float BrightnessMin { get; set; } = 0.88f;
    [Export(PropertyHint.Range, "0.5,1.2,0.01")] public float BrightnessMax { get; set; } = 1.0f;
}
