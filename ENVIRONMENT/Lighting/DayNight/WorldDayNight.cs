// Shared clock-driven illumination with cached solar curves and bounded static shadow projection error.
using Godot;
public partial class WorldDayNight : Node2D
{
    public DayNightProfile Config { get; set; }
    public GameClock Clock { get; set; }
    public WorldWeather Weather { get; set; }
    public SunShadowRenderer Shadows { get; set; }
    public float Daylight { get; private set; }
    public int SolarCalculations { get; private set; }
    public int AmbientWrites { get; private set; }
    private CanvasModulate _ambient;
    private bool _settingsDirty=true, _ambientApplied, _lastPaused;
    private ulong _lastRevision;
    private float _lastHour=float.NaN, _length, _angle, _opacity;
    private Color _baseAmbient, _lastAmbient;
    // Profile edits invalidate solar settings; the frame callback still services actors/camera/weather.
    // =========================================================
    public override void _Ready()
    { _ambient=new CanvasModulate(); AddChild(_ambient); Config.Changed+=SettingsChanged; }
    private void SettingsChanged() => _settingsDirty=true;
    // Paused solar inputs reuse curves; weather/lightning remain responsive independently.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace=RuntimeFrameTrace.Measure("WorldDayNight.Process");
        float hour=Clock.Hour;
        bool force=_settingsDirty || _lastRevision!=Clock.ManualRevision || _lastPaused!=Clock.Config.Paused;
        if(_settingsDirty || _lastHour!=hour)
        {
            SolarCalculations++; _lastHour=hour;
            float altitude=SunCycle.Altitude(hour);
            Daylight=SunCycle.Edge(-0.12f,0.20f,altitude);
            float noon=SunCycle.Edge(0,0.70f,Mathf.Max(0,altitude));
            _baseAmbient=Config.NightColour.Lerp(Config.HorizonColour.Lerp(Config.NoonColour,noon),Daylight);
            _length=Mathf.Lerp(Config.MaximumLength,Config.MinimumLength,Mathf.Sqrt(Mathf.Max(0,altitude)));
            float progress=Mathf.Clamp((hour-6f)/12f,0,1);
            _angle=Mathf.DegToRad(Mathf.Lerp(150,30,progress)+Config.SunDirectionOffsetDegrees);
            _opacity=Config.ShadowOpacity*SunCycle.Edge(0,0.18f,altitude);
        }
        Color ambient=_baseAmbient*Colors.White.Lerp(Weather.Config.OvercastTint,Weather.CloudCover*Daylight);
        ambient=ambient.Lerp(Weather.Config.LightningTint,Weather.Lightning.Flash);
        if(!_ambientApplied || _lastAmbient!=ambient) { _ambient.Color=ambient; _lastAmbient=ambient; _ambientApplied=true; AmbientWrites++; }
        float opacity=_opacity*(1f-Weather.CloudCover*Mathf.Clamp(Weather.Config.OvercastShadowReduction,0,1));
        Shadows.UpdateProjection(_angle,_length,opacity,Config.SoftShadows?Config.ShadowSoftness:0,
            Config.ShadowProjectionTolerancePixels,force);
        _settingsDirty=false; _lastRevision=Clock.ManualRevision; _lastPaused=Clock.Config.Paused;
    }
    public override void _ExitTree() => Config.Changed-=SettingsChanged;
}
