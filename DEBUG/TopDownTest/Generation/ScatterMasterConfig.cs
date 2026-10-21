// Bounded, reproducible scenery generation settings for the disposable 2D test map.
using Godot;

[GlobalClass]
public partial class ScatterMasterConfig : Resource
{
    [ExportGroup("World")]
    [Export] public Rect2 WorldBounds { get; set; } = new(-480f, -320f, 1920f, 1280f);
    [Export] public int Seed { get; set; } = 213876;

    [ExportGroup("Population")]
    [Export(PropertyHint.Range, "0,150,1")] public int TreeCount { get; set; } = 58;
    [Export(PropertyHint.Range, "0,100,1")] public int RockCount { get; set; } = 24;
    [Export(PropertyHint.Range, "1,120,1")] public int AttemptsPerObject { get; set; } = 65;

    [ExportGroup("Variation")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float MirrorChance { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0.7,1.3,0.01")] public float TreeScaleMin { get; set; } = 0.93f;
    [Export(PropertyHint.Range, "0.7,1.3,0.01")] public float TreeScaleMax { get; set; } = 1.07f;
    [Export(PropertyHint.Range, "0.7,1.3,0.01")] public float RockScaleMin { get; set; } = 0.90f;
    [Export(PropertyHint.Range, "0.7,1.3,0.01")] public float RockScaleMax { get; set; } = 1.10f;

    [ExportGroup("Clearance / collisions")]
    [Export] public Vector2 PlayerSpawn { get; set; } = new(480f, 320f);
    [Export(PropertyHint.Range, "60,300,5")] public float StartClearingRadius { get; set; } = 180f;
    [Export(PropertyHint.Range, "0,100,5")] public float PathHalfWidth { get; set; } = 27f;
    [Export(PropertyHint.Range, "20,120,5")] public float EdgeMargin { get; set; } = 75f;
    [Export(PropertyHint.Range, "10,90,5")] public float TreeSpacingRadius { get; set; } = 55f;
    [Export(PropertyHint.Range, "10,90,5")] public float RockSpacingRadius { get; set; } = 32f;
    [Export(PropertyHint.Range, "0,40,1")] public float ExtraSpacing { get; set; } = 8f;
}
