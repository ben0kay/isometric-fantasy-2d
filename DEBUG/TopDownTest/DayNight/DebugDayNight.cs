// One debug controller owns the clock, ambient tint and cached projected sprite shadows.
using Godot;
using System.Collections.Generic;

public partial class DebugDayNight : Node2D
{
    [Export] public DayNightMasterConfig Config { get; set; }
    private sealed class Caster
    {
        public Node2D Body;
        public Sprite2D Source;
        public Sprite2D Shadow;
    }
    private readonly List<Caster> _casters = new();
    private readonly List<ShaderMaterial> _windMaterials = new();
    private readonly HashSet<ShaderMaterial> _shadowMaterials = new();
    private DebugWeather _weather;
    private CanvasModulate _ambient;
    private Label _clock;
    private HSlider _slider;
    private CheckButton _pause;
    public float Daylight { get; private set; }
    private float _hour;
    private bool _updatingSlider;

    #region Setup
    // Build only visual shadow helpers; authored scenery and collision are untouched.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new DayNightMasterConfig();
        _weather = GetNode<DebugWeather>("../Weather");
        _hour = Mathf.PosMod(Config.StartHour, 24f);
        _ambient = GetNode<CanvasModulate>("Ambient");
        _clock = GetNode<Label>("UI/Panel/Controls/Clock");
        _slider = GetNode<HSlider>("UI/Panel/Controls/Time");
        _pause = GetNode<CheckButton>("UI/Panel/Controls/Pause");
        _slider.ValueChanged += value => { if (!_updatingSlider) { _hour = (float)value; UpdateLighting(); } };
        Node2D actors = GetNode<Node2D>("../Actors");
        Node2D shadows = GetNode<Node2D>("Shadows");
        Shader flatShader = GD.Load<Shader>("res://DEBUG/TopDownTest/DayNight/Silhouette.gdshader");
        var flatMaterial = new ShaderMaterial { Shader = flatShader };
        var sharedWind = new Dictionary<Material, ShaderMaterial>();
        var sharedWindShadows = new Dictionary<Material, ShaderMaterial>();
        foreach (Node child in actors.GetChildren())
        {
            if (child is not Node2D body) continue;
            Sprite2D source = body.GetNodeOrNull<Sprite2D>("Sprite");
            if (source == null) continue;
            Material shadowMaterial = flatMaterial;
            if (source.Material is ShaderMaterial original)
            {
                if (!sharedWind.TryGetValue(original, out ShaderMaterial litWind))
                {
                    litWind = (ShaderMaterial)original.Duplicate();
                    sharedWind.Add(original, litWind);
                    _windMaterials.Add(litWind);
                }
                source.Material = litWind;
                source.SetInstanceShaderParameter("wind_orientation", source.FlipH ? -1f : 1f);
                if (!sharedWindShadows.TryGetValue(original, out ShaderMaterial windShadow))
                {
                    windShadow = (ShaderMaterial)litWind.Duplicate();
                    windShadow.SetShaderParameter("shadow_only", true);
                    sharedWindShadows.Add(original, windShadow);
                    _windMaterials.Add(windShadow);
                }
                shadowMaterial = windShadow;
            }
            if (shadowMaterial is ShaderMaterial cachedShadow) _shadowMaterials.Add(cachedShadow);
            var shadow = new Sprite2D
            {
                Name = body.Name + "SunShadow", Texture = source.Texture,
                Centered = source.Centered, Offset = source.Offset,
                FlipH = source.FlipH, FlipV = source.FlipV,
                TextureFilter = source.TextureFilter, Material = shadowMaterial
            };
            shadows.AddChild(shadow);
            if (source.Material is ShaderMaterial)
                shadow.SetInstanceShaderParameter("wind_orientation", source.FlipH ? -1f : 1f);
            _casters.Add(new Caster { Body = body, Source = source, Shadow = shadow });
            // The player already has a contact shadow; trees and rocks get matching grounding.
            if (body.GetNodeOrNull<Polygon2D>("Shadow") == null)
                AddContactShadow(body, shadows, child.Name.ToString().StartsWith("Tree"));
        }
        UpdateLighting();
    }

    // Add a low-cost, stationary oval under a static tree or rock.
    // =========================================================
    private void AddContactShadow(Node2D body, Node2D shadows, bool tree)
    {
        var points = new Vector2[16];
        // Root scaling is used for variations, so also scale the generated contact oval.
        Vector2 radius = (tree ? new Vector2(25, 9) : new Vector2(28, 8)) * body.Scale.Abs();
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length;
            points[i] = new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
        shadows.AddChild(new Polygon2D
        {
            Name = body.Name + "Contact", Position = body.Position + new Vector2(0, -3),
            Polygon = points, Color = new Color(0.055f, 0.075f, 0.06f, 0.23f)
        });
    }
    #endregion

    #region Simulation
    // A complete 24-hour cycle takes CycleSeconds; pausing still allows time scrubbing.
    // =========================================================
    public override void _Process(double delta)
    {
        if (!_pause.ButtonPressed)
            _hour = Mathf.PosMod(_hour + (float)delta * 24f / Mathf.Max(1f, Config.CycleSeconds), 24f);
        UpdateLighting();
    }

    // Reuse cached casters/materials rather than searching scene nodes each frame.
    // =========================================================
    private void UpdateLighting()
    {
        float altitude = Mathf.Sin((_hour - 6f) / 24f * Mathf.Tau);
        float daylight = Edge(-0.12f, 0.20f, altitude);
        Daylight = daylight;
        float noon = Edge(0f, 0.70f, Mathf.Max(0, altitude));
        Color ambient = Config.NightColour.Lerp(Config.HorizonColour.Lerp(Config.NoonColour, noon), daylight);
        _ambient.Color = ambient * Colors.White.Lerp(_weather.Config.OvercastTint, _weather.CloudCover * daylight);
        float length = Mathf.Lerp(Config.MaximumLength, Config.MinimumLength, Mathf.Sqrt(Mathf.Max(0, altitude)));
        float dayProgress = Mathf.Clamp((_hour - 6f) / 12f, 0f, 1f);
        float angle = Mathf.DegToRad(Mathf.Lerp(150f, 30f, dayProgress) + Config.SunDirectionOffsetDegrees);
        Vector2 direction = Vector2.FromAngle(angle);
        var projection = new Transform2D(Vector2.Right, -direction * Mathf.Max(0.01f, length), Vector2.Zero);
        float opacity = Config.ShadowOpacity * Edge(0f, 0.18f, altitude) * (1f - _weather.CloudCover * 0.85f);
        foreach (Caster caster in _casters)
        {
            // Preserve the static body's local scale/rotation so size-varied trees and rocks
            // project silhouettes matching their actual sprite and collision footprints.
            Transform2D bodyBasis = caster.Body.Transform;
            bodyBasis.Origin = Vector2.Zero;
            caster.Shadow.Transform = new Transform2D(0f, caster.Body.Position) * projection * bodyBasis * caster.Source.Transform;
            caster.Shadow.Modulate = new Color(1, 1, 1, opacity);
            caster.Shadow.Visible = opacity > 0.001f && caster.Source.IsVisibleInTree();
        }
        foreach (ShaderMaterial material in _windMaterials)
            material.SetShaderParameter("wind_multiplier", _weather.WindStrength);
        _updatingSlider = true;
        _slider.Value = _hour;
        _updatingSlider = false;
        int hours = (int)_hour;
        int minutes = (int)((_hour - hours) * 60);
        _clock.Text = $"{hours:00}:{minutes:00}  |  {Config.CycleSeconds:0}s per day";
    }
    // Called by the optional polish preview; modify cached shadow materials only.
    // =========================================================
    public void SetShadowSoftness(float softness)
    {
        foreach (ShaderMaterial material in _shadowMaterials)
            material.SetShaderParameter("softness_uv", softness);
    }

    // Smoothly map a value between two thresholds (unlike value-interpolating Mathf.SmoothStep).
    // =========================================================
    private static float Edge(float low, float high, float value)
    {
        float t = Mathf.Clamp((value - low) / (high - low), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
    #endregion
}
