// Illumination consumes the authoritative clock, weather and cached shadow renderer.
using Godot;
public partial class WorldDayNight : Node2D
{
    public DayNightProfile Config { get; set; }
    public GameClock Clock { get; set; }
    public WorldWeather Weather { get; set; }
    public SunShadowRenderer Shadows { get; set; }
    public float Daylight { get; private set; }
    private CanvasModulate _ambient;
    // Ambient modulation is independent of optional developer controls.
    // =========================================================
    public override void _Ready() { _ambient = new CanvasModulate(); AddChild(_ambient); }
    // Apply original noon/horizon/night colours, overcast and short lightning flashes.
    // =========================================================
    public override void _Process(double delta)
    {
        float hour = Clock.Hour, altitude = SunCycle.Altitude(hour);
        Daylight = SunCycle.Edge(-0.12f, 0.20f, altitude);
        float noon = SunCycle.Edge(0f, 0.70f, Mathf.Max(0, altitude));
        Color ambient = Config.NightColour.Lerp(Config.HorizonColour.Lerp(Config.NoonColour, noon), Daylight);
        _ambient.Color = ambient * Colors.White.Lerp(Weather.Config.OvercastTint, Weather.CloudCover * Daylight);
        _ambient.Color = _ambient.Color.Lerp(Weather.Config.LightningTint, Weather.Lightning.Flash);
        float length = Mathf.Lerp(Config.MaximumLength, Config.MinimumLength, Mathf.Sqrt(Mathf.Max(0, altitude)));
        float dayProgress = Mathf.Clamp((hour - 6f) / 12f, 0f, 1f);
        float angle = Mathf.DegToRad(Mathf.Lerp(150f, 30f, dayProgress) + Config.SunDirectionOffsetDegrees);
        float opacity = Config.ShadowOpacity * SunCycle.Edge(0f, 0.18f, altitude) * (1f - Weather.CloudCover * 0.85f);
        Shadows.UpdateProjection(angle, length, opacity, Config.SoftShadows ? Config.ShadowSoftness : 0f);
    }
}
