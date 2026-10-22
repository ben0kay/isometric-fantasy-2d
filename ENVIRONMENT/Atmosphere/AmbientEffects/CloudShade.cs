// Shared world-space cloud shade, driven by integrated wind and daylight.
using Godot;
public partial class CloudShade : Polygon2D
{
    public CloudShadeProfile Config { get; set; }
    public WorldDayNight Day { get; set; }
    public WindController Wind { get; set; }
    private ShaderMaterial _material;
    private float _lastOpacity = float.NaN, _lastDaylight = float.NaN;
    private Vector2 _lastDrift = new(float.NaN,float.NaN);
    public int MaterialWrites { get; private set; }
    // Configure the overlay to the same world bounds as terrain and scenery.
    // =========================================================
    public void Configure(Rect2 world)
    {
        Polygon = new[] { world.Position, new Vector2(world.End.X, world.Position.Y), world.End, new Vector2(world.Position.X, world.End.Y) };
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://ENVIRONMENT/Atmosphere/AmbientEffects/CloudShade.gdshader") };
        _material.SetShaderParameter("atmosphere_noise_texture", GD.Load<Texture2D>("res://ENVIRONMENT/Atmosphere/AtmosphereProfiles/AtmosphereNoise.tres"));
        Material = _material; ZIndex = 5;
    }
    // Profile signals can wake this overlay without an idle polling callback.
    // =========================================================
    public override void _Ready() { Config.Changed += RefreshState; RefreshState(); }
    private void RefreshState()
    {
        bool active = Config.Enabled && Config.Opacity > 0;
        if (Visible != active) Visible = active;
        SetProcess(active);
        if (active) ApplyState();
    }
    // Active drift/daylight may change continuously; static inputs make no repeated GPU writes.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("CloudShade.Process");
        if (Config.Enabled && Config.Opacity > 0) ApplyState();
    }
    private void ApplyState()
    {
        if (_lastOpacity != Config.Opacity)
        { _lastOpacity = Config.Opacity; _material.SetShaderParameter("opacity",_lastOpacity); MaterialWrites++; }
        Vector2 drift = -Wind.Displacement;
        if (_lastDrift != drift)
        { _lastDrift = drift; _material.SetShaderParameter("drift",drift); MaterialWrites++; }
        if (_lastDaylight != Day.Daylight)
        { _lastDaylight = Day.Daylight; _material.SetShaderParameter("daylight",_lastDaylight); MaterialWrites++; }
    }
    // Disconnect the live Resource when the world/overlay is removed.
    // =========================================================
    public override void _ExitTree() => Config.Changed -= RefreshState;
}
