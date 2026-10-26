// Optional new-world guidance; frozen outcomes live in the blueprint, never in this mutable Resource.
using Godot;
using System;
[GlobalClass]
public partial class WorldAuthorPreset : Resource
{
    [ExportGroup("Broad ocean placement - normalized centre zone, not exact centre")]
    [Export] public Rect2 OceanCentreZone { get; set; } = new(0.25f,0.25f,0.5f,0.5f);
    [Export] public bool RequireOceanInZone { get; set; }
    [ExportGroup("North to south climate - baseline only")]
    [Export] public float NorthTemperatureCelsius { get; set; } = -24;
    [Export] public float SouthTemperatureCelsius { get; set; } = 38;
    [Export] public float NorthHumidityPercent { get; set; } = 65;
    [Export] public float SouthHumidityPercent { get; set; } = 20;
    [Export(PropertyHint.Range,"0,0.2")] public float BoundaryWarp { get; set; } = 0.09f;
    [ExportGroup("Optional enchanted province")]
    [Export] public bool EnchantedEnabled { get; set; }
    [Export] public Vector2 EnchantedAnchor { get; set; } = new(0.8f,0.4f);
    [Export] public Vector2 EnchantedExtent { get; set; } = new(0.12f,0.16f);
    [Export(PropertyHint.Range,"0.01,1")] public float EnchantedTransition { get; set; } = 0.3f;
    [ExportGroup("Initial middle start - region selector remains BC5")]
    [Export] public Rect2 StartZone { get; set; } = new(0.05f,0.35f,0.9f,0.3f);
    [Export] public float MajorPoiClearance { get; set; } = 2000;
    [Export] public float ShoreClearance { get; set; } = 256;
    [Export] public int StartAttempts { get; set; } = 128;

    // Reject invalid guidance before any map allocations; absent preset retains the legacy path.
    // =========================================================
    public void Validate()
    {
        ValidateZone(OceanCentreZone); ValidateZone(StartZone);
        if (!float.IsFinite(MajorPoiClearance) || MajorPoiClearance<0 || MajorPoiClearance>100000
            || !float.IsFinite(ShoreClearance) || ShoreClearance<0 || ShoreClearance>100000 || StartAttempts<1 || StartAttempts>256)
            throw new ArgumentException("Invalid bounded World Author spawn policy.");
        WorldClimatePlan.FromPreset(this,0,new Rect2(0,0,1,1));
    }
    // Normalized anchors remain valid as finite world bounds change.
    // =========================================================
    private static void ValidateZone(Rect2 zone)
    {
        if (!zone.Position.IsFinite() || !zone.Size.IsFinite() || zone.Size.X<=0 || zone.Size.Y<=0
            || zone.Position.X<0 || zone.Position.Y<0 || zone.End.X>1 || zone.End.Y>1)
            throw new ArgumentException("World Author zones must fit normalized 0..1 bounds.");
    }
}
