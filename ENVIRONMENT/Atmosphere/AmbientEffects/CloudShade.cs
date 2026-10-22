// Shared world-space cloud shade, driven by integrated wind and daylight.
using Godot;
public partial class CloudShade : Polygon2D
{
    public CloudShadeProfile Config { get; set; }
    public WorldDayNight Day { get; set; }
    public WindController Wind { get; set; }
    private ShaderMaterial _material;
    // Configure the overlay to the same world bounds as terrain and scenery.
    // =========================================================
    public void Configure(Rect2 world)
    {
        Polygon = new[] { world.Position, new Vector2(world.End.X, world.Position.Y), world.End, new Vector2(world.Position.X, world.End.Y) };
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://ENVIRONMENT/Atmosphere/AmbientEffects/CloudShade.gdshader") };
        _material.SetShaderParameter("atmosphere_noise_texture", GD.Load<Texture2D>("res://ENVIRONMENT/Atmosphere/AtmosphereProfiles/AtmosphereNoise.tres"));
        Material = _material; ZIndex = 5;
    }
    // Drift continuously when the live wind strength changes.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("CloudShade.Process");
        Visible = Config.Enabled;
        _material.SetShaderParameter("opacity", Config.Opacity);
        _material.SetShaderParameter("drift", -Wind.Displacement);
        _material.SetShaderParameter("daylight", Day.Daylight);
    }
}
