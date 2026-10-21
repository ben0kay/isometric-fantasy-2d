// All settings for the disposable daylight, wind and shadow demonstration.
using Godot;

[GlobalClass]
public partial class DayNightMasterConfig : Resource
{
    [ExportGroup("Clock")]
    [Export(PropertyHint.Range, "5,600,1")] public float CycleSeconds { get; set; } = 30f;
    [Export(PropertyHint.Range, "0,24,0.1")] public float StartHour { get; set; } = 8f;
    [ExportGroup("Ambient colours")]
    [Export] public Color NoonColour { get; set; } = new(1f, 0.98f, 0.91f);
    [Export] public Color HorizonColour { get; set; } = new(0.83f, 0.56f, 0.38f);
    [Export] public Color NightColour { get; set; } = new(0.18f, 0.23f, 0.36f);
    [ExportGroup("Sun shadows")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float ShadowOpacity { get; set; } = 0.36f;
    [Export] public float MinimumLength { get; set; } = 0.18f;
    [Export] public float MaximumLength { get; set; } = 1.65f;
    [Export] public float SunDirectionOffsetDegrees { get; set; } = 0f;
    [ExportGroup("Shared test wind")]
    [Export(PropertyHint.Range, "0,3,0.05")] public float WindStrengthMultiplier { get; set; } = 1f;
}
