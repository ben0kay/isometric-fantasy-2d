// Optional shared local spacing settings; omitted profiles use these defaults.
using Godot;
[GlobalClass]
public partial class SeparationProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range,"0,40,1")] public float Clearance { get; set; } = 10;
    [Export(PropertyHint.Range,"0,0.5,0.05")] public float Strength { get; set; } = 0.35f;
    // Clamp malformed authored values without allowing NaN into movement.
    // =========================================================
    public static float Bounded(float value,float fallback,float min,float max)
        => float.IsFinite(value) ? Mathf.Clamp(value,min,max) : fallback;
}
