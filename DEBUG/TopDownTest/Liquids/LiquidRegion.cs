// One authoritative polygon supplies rendering distance, placement exclusion and wading depth.
using Godot;

public partial class LiquidRegion : Node2D
{
    [Export] public LiquidMasterConfig Config { get; set; }
    [Export] public LiquidDefinition Definition { get; set; }
    public bool Active => Config.Enabled;
    public Vector2[] Boundary { get; private set; }
    private Vector2[] _edgeDelta;
    private float[] _inverseLength;
    private ShaderMaterial _material;
    private MeshInstance2D _surface;
    private float _time;
    private Vector2 _windDrift;

    #region Startup geometry
    // Register before scenery generation; the parent test scene is placed before Actors.
    // =========================================================
    public override void _EnterTree() => AddToGroup("debug_liquid_regions");

    // Moderate harmonic perimeter perturbation produces a smooth, stable non-self-crossing lake.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new LiquidMasterConfig(); Definition ??= new LiquidDefinition();
        Position = Config.Centre;
        Boundary = new Vector2[96]; _edgeDelta = new Vector2[96]; _inverseLength = new float[96];
        float phase = (Config.ShapeSeed % 997) * 0.01f;
        Vector2 radius = Config.Radius.Abs().Max(Vector2.One * 40f);
        for (int i = 0; i < Boundary.Length; i++)
        {
            float angle = Mathf.Tau * i / Boundary.Length;
            float scale = 1f + 0.12f * Mathf.Sin(angle * 3f + phase) + 0.065f * Mathf.Cos(angle * 5f - phase) + 0.035f * Mathf.Sin(angle * 7f);
            Boundary[i] = Vector2.FromAngle(angle) * radius * scale;
        }
        Rect2 bounds = new(Boundary[0], Vector2.Zero);
        for (int i = 0; i < Boundary.Length; i++)
        {
            _edgeDelta[i] = Boundary[(i + 1) % Boundary.Length] - Boundary[i];
            _inverseLength[i] = 1f / Mathf.Max(0.001f, _edgeDelta[i].LengthSquared());
            bounds = bounds.Expand(Boundary[i]);
        }
        bounds = bounds.Grow(40f);
        Texture2D field = BuildDistanceField(bounds);
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://DEBUG/TopDownTest/Liquids/Shaders/LiquidSurface.gdshader") };
        _material.SetShaderParameter("distance_field", field);
        _material.SetShaderParameter("surface_noise", GD.Load<Texture2D>("res://DEBUG/TopDownTest/Ground/Noise/GroundNoise.tres"));
        _surface = new MeshInstance2D { Name = "Surface", Position = bounds.GetCenter(), Mesh = new QuadMesh { Size = bounds.Size }, Material = _material, ZIndex = -6 };
        AddChild(_surface);
    }

    // Rasterize only once: shaders sample distance directly, never loop over shoreline segments.
    // =========================================================
    private Texture2D BuildDistanceField(Rect2 bounds)
    {
        const int width = 384, height = 256;
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rf);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            Vector2 p = bounds.Position + new Vector2((x + 0.5f) / width, (y + 0.5f) / height) * bounds.Size;
            float distance = SignedLocalDistance(p);
            image.SetPixel(x, y, new Color(Mathf.Clamp(0.5f + distance / 512f, 0f, 1f), 0, 0));
        }
        return ImageTexture.CreateFromImage(image);
    }
    #endregion

    #region Shared footprint queries
    // Positive distances are underwater; negative distances lie outside the authoritative polygon.
    // =========================================================
    public float SignedDistance(Vector2 world) => Boundary == null ? -100000f : SignedLocalDistance(ToLocal(world));

    // Margin reserves ground outside the water too, for tree roots, rock gravel and shoreline grass.
    // =========================================================
    public bool Contains(Vector2 world, float margin = 0f) => SignedDistance(world) >= -Mathf.Max(0f, margin);

    // Ray crossings and nearest edge distance share one bounded loop with no query allocations.
    // =========================================================
    private float SignedLocalDistance(Vector2 p)
    {
        bool inside = false;
        float minimum = float.MaxValue;
        for (int i = 0; i < Boundary.Length; i++)
        {
            Vector2 a = Boundary[i], b = Boundary[(i + 1) % Boundary.Length], delta = _edgeDelta[i];
            float t = Mathf.Clamp((p - a).Dot(delta) * _inverseLength[i], 0f, 1f);
            minimum = Mathf.Min(minimum, p.DistanceSquaredTo(a + delta * t));
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return Mathf.Sqrt(minimum) * (inside ? 1f : -1f);
    }
    #endregion

    #region Presentation
    // Read shared weather without modifying it; geometry and masks never rebuild each frame.
    // =========================================================
    public void UpdateSurface(double delta, float wind)
    {
        Visible = Active;
        if (Config.Animate)
        {
            _time += (float)delta * Definition.WaveSpeed;
            _windDrift.X += wind * (float)delta * 8f;
        }
        _material.SetShaderParameter("wind_drift", _windDrift);
        _material.SetShaderParameter("surface_time", _time);
        _material.SetShaderParameter("wind_strength", wind);
        _material.SetShaderParameter("ripples", Config.RippleStrength);
        _material.SetShaderParameter("shore_width", Config.Shoreline ? Mathf.Clamp(Config.ShoreWidth, 0f, 28f) : 0f);
        _material.SetShaderParameter("shallow_colour", Definition.ShallowColour);
        _material.SetShaderParameter("deep_colour", Definition.DeepColour);
        _material.SetShaderParameter("highlight_colour", Definition.HighlightColour);
        _material.SetShaderParameter("bank_colour", Definition.BankColour);
        _material.SetShaderParameter("depth_distance", Mathf.Max(10f, Definition.DepthDistance));
    }
    #endregion
}
