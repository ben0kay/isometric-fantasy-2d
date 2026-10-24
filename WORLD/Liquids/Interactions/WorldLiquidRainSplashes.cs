// Pooled rain-impact rings on any debug liquid region, driven by existing Weather rain.
// Mirrors the dry-ground splash effect without extra physics bodies or per-drop nodes.
using Godot;

public partial class WorldLiquidRainSplashes : Node2D
{
    public WorldLiquidRegion Region { get; set; }
    public WorldWeather Weather { get; set; }
    public WorldLiquidMasterConfig Config { get; set; }

    private struct Impact { public Vector2 Position; public float Age; public float Size; public bool Active; }
    private readonly Impact[] _pool = new Impact[96];
    private readonly RandomNumberGenerator _random = new() { Seed = 58047 };
    private float _budget;
    private int _next;
    private bool _drewLastFrame;

    // Intensity changes wake this pool; dry weather never polls the lake each frame.
    // =========================================================
    public override void _Ready() { Weather.RainStateChanged += ApplyRainState; Config.Changed += ApplyRainState; ApplyRainState(); }
    public void ApplyRainState()
    {
        bool active = Weather.RainActive && Region.PresentationActive && Region.Active && Config.RainSurfaceSplashes; SetProcess(active); Visible = active;
        if (!active) { System.Array.Clear(_pool); _budget=0; _next=0; _drewLastFrame=false; QueueRedraw(); }
    }
    public override void _ExitTree() { Weather.RainStateChanged -= ApplyRainState; Config.Changed -= ApplyRainState; }
    #region Impact simulation
    // Only the small lake rectangle is sampled; polygon distance rejects land and the shore.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldLiquidRainSplashes.Process");
        if (!GodotObject.IsInstanceValid(Region) || Config == null || Weather == null) return;
        if (!Region.Active || !Config.RainSurfaceSplashes)
        {
            _budget = 0f;
            bool hadImpacts = _drewLastFrame;
            for (int i = 0; i < _pool.Length; i++) _pool[i].Active = false;
            _drewLastFrame = false;
            if (hadImpacts) QueueRedraw();
            return;
        }

        float lifetime = Mathf.Clamp(Config.RainSplashLifetime, 0.2f, 1.2f);
        bool visible = false;
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].Active) continue;
            _pool[i].Age += (float)delta;
            if (_pool[i].Age >= lifetime) _pool[i].Active = false;
            else visible = true;
        }

        // Rain uses its existing fade and slider. The lake's own toggle is independent
        // of the "Ground splashes" checkbox, which remains dry-ground-only.
        if (Weather.Rain > 0.001f)
        {
            Rect2 area = Weather.VisibleWorldRect().Intersection(Region.SurfaceBounds);
            if (area.HasArea())
            {
                float rate = Mathf.Clamp(Config.RainSplashRate, 0f, 180f) * Weather.Rain;
                _budget = Mathf.Min(8f, _budget + (float)delta * rate);
                while (_budget >= 1f)
                {
                    _budget -= 1f;
                    if (TrySpawn(area)) visible = true;
                }
            }
            else _budget = 0f; // Offscreen lakes spend no impact budget.
        }
        else _budget = 0f;

        if (visible || _drewLastFrame) QueueRedraw();
        _drewLastFrame = visible;
    }

    // Uniformly sample the camera-visible part of the lake, never the surrounding ground.
    // =========================================================
    private bool TrySpawn(Rect2 visibleLakeBounds)
    {
        float size = Mathf.Clamp(Config.RainSplashSize, 0.5f, 2f);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 world = visibleLakeBounds.Position
                + new Vector2(_random.Randf(), _random.Randf()) * visibleLakeBounds.Size;
            // Positive signed distance is underwater. Keep expanding rings within the lake.
            if (Region.SignedDistance(world) <= 9f * size) continue;
            _pool[_next] = new Impact
            {
                Position = Region.ToLocal(world),
                Size = size * _random.RandfRange(0.8f, 1.2f),
                Age = 0f,
                Active = true
            };
            _next = (_next + 1) % _pool.Length;
            return true;
        }
        return false;
    }
    #endregion

    #region Appearance
    // Tiny expanding ellipse, secondary echo and brief upright spray match the ground style.
    // =========================================================
    public override void _Draw()
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldLiquidRainSplashes.Draw");
        if (Region == null || Config == null || !Region.Active || !Config.RainSurfaceSplashes) return;
        float lifetime = Mathf.Clamp(Config.RainSplashLifetime, 0.2f, 1.2f);
        float opacity = Mathf.Clamp(Config.RainSplashOpacity, 0f, 1f);
        Color tint = Region.Definition.HighlightColour;
        foreach (Impact impact in _pool)
        {
            if (!impact.Active) continue;
            float t = Mathf.Clamp(impact.Age / lifetime, 0f, 1f);
            float radius = (0.7f + t * 6.0f) * impact.Size;
            float fade = (1f - t) * (1f - t) * opacity;
            DrawSetTransform(impact.Position, 0f, new Vector2(1f, 0.48f));
            DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 14, new Color(tint, fade), 0.85f, true);
            if (t > 0.22f)
                DrawArc(Vector2.Zero, Mathf.Max(0.5f, radius - 2.2f * impact.Size),
                    0f, Mathf.Tau, 12, new Color(tint, fade * 0.28f), 0.6f, true);
            DrawSetTransform(impact.Position);
            if (t < 0.42f)
            {
                float spray = Mathf.Sin(t / 0.42f * Mathf.Pi) * 2.5f * impact.Size;
                DrawLine(new Vector2(-0.8f, -spray), new Vector2(-1.2f, -spray - 1.1f),
                    new Color(tint, fade * 0.7f), 0.6f, true);
                DrawLine(new Vector2(0.9f, -spray), new Vector2(1.4f, -spray - 0.9f),
                    new Color(tint, fade * 0.65f), 0.6f, true);
            }
        }
        DrawSetTransform(Vector2.Zero);
    }
    #endregion
}
