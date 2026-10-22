// Draws a bounded splash pool below every actor, excluding static prop artwork rectangles.
using Godot;
using System.Collections.Generic;

public partial class GroundRainSplashes : Node2D
{
    public WorldWeather Weather { get; set; }
    private struct Splash { public Vector2 Position; public float Age; public bool Active; }
    private readonly Splash[] _pool = new Splash[96];
    private readonly RandomNumberGenerator _random = new() { Seed = 71391 };
    public Node2D Actors { get; set; }
    public Rect2 WorldBounds { get; set; }
    private float _budget;
    private int _next;

    #region Setup
    // Static props are already generated; cache conservative artwork bounds once, not per frame.
    // =========================================================
    public override void _Ready()
    {
        // Match rain visibility at night without a separate light for every splash.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        SetRainActive(Weather?.RainActive == true);
    }

    // A zero target stops the callback and removes already drawn impacts immediately.
    // =========================================================
    public void SetRainActive(bool active)
    {
        bool run = active && Weather.Config.Splashes;
        SetProcess(run); Visible = run;
        if (!run) { System.Array.Clear(_pool); _budget = 0; _next = 0; QueueRedraw(); }
    }
    #endregion

    #region Simulation and drawing
    // Bounded attempts and a reusable pool keep dense forests and frame stalls inexpensive.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundRainSplashes.Process");
        for (int i = 0; i < _pool.Length; i++)
            if (_pool[i].Active) { _pool[i].Age += (float)delta; if (_pool[i].Age >= Mathf.Clamp(Weather.Config.SplashLifetime, 0.15f, 0.6f)) _pool[i].Active = false; }
        if (Weather.Config.Splashes && Weather.Rain > 0.001f)
        {
            _budget = Mathf.Min(8f, _budget + (float)delta * Mathf.Clamp(Weather.Config.SplashesPerSecond, 0f, 200f) * Weather.Rain);
            Rect2 area = Weather.VisibleWorldRect().Intersection(WorldBounds);
            Rect2 player = Weather.GroundQueries.PlayerBounds(8);
            while (_budget >= 1f && area.HasArea())
            {
                _budget -= 1f;
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    Vector2 p = area.Position + new Vector2(_random.Randf(), _random.Randf()) * area.Size;
                    if (!Weather.GroundQueries.IsClear(p, player)) continue;
                    _pool[_next] = new Splash { Position = p, Active = true };
                    _next = (_next + 1) % _pool.Length;
                    break;
                }
            }
        }
        else _budget = 0;
        QueueRedraw();
    }

    // These are ground-level arcs and tiny upward droplets, never surface hits on scenery.
    // =========================================================
    public override void _Draw()
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundRainSplashes.Draw");
        if (!Weather.Config.Splashes) return;
        foreach (Splash splash in _pool)
        {
            if (!splash.Active) continue;
            float t = Mathf.Clamp(splash.Age / Mathf.Clamp(Weather.Config.SplashLifetime, 0.15f, 0.6f), 0f, 1f);
            float size = Mathf.Clamp(Weather.Config.SplashSize, 0.5f, 2f), radius = (0.5f + t * 2.5f) * size;
            var colour = new Color(0.72f, 0.81f, 0.86f, (1f - t) * Mathf.Sqrt(Weather.Rain) * Mathf.Clamp(Weather.Config.SplashOpacity, 0f, 1f));
            DrawSetTransform(splash.Position, 0, new Vector2(1, 0.4f));
            DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 10, colour, 0.8f, true);
            DrawSetTransform(splash.Position);
            float rise = Mathf.Sin(t * Mathf.Pi) * 3f * size;
            DrawLine(new Vector2(-radius, -rise), new Vector2(-radius - 0.4f, -rise - 0.9f), colour, 0.7f, true);
            DrawLine(new Vector2(radius, -rise), new Vector2(radius + 0.4f, -rise - 0.9f), colour, 0.5f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }
    #endregion
}
