// Draws a bounded splash pool below every actor, excluding static prop artwork rectangles.
using Godot;
using System.Collections.Generic;

public partial class DebugRainSplashes : Node2D
{
    public DebugWeather Weather { get; set; }
    private struct Splash { public Vector2 Position; public float Age; public bool Active; }
    private readonly Splash[] _pool = new Splash[96];
    private readonly List<Rect2> _blocked = new();
    private readonly RandomNumberGenerator _random = new() { Seed = 71391 };
    private Sprite2D _player;
    private Rect2 _world;
    private float _budget;
    private int _next;

    #region Setup
    // Static props are already generated; cache conservative artwork bounds once, not per frame.
    // =========================================================
    public override void _Ready()
    {
        Node2D actors = GetNode<Node2D>("../../Actors");
        _world = ((DebugWorldScatter)actors).Config.WorldBounds;
        foreach (Node child in actors.GetChildren())
        {
            if (child.Name == "Player") { _player = child.GetNode<Sprite2D>("Sprite"); continue; }
            CacheArtwork(child);
        }
    }

    // Includes polygon-authored torch artwork as well as tree/rock sprites, with sway clearance.
    // =========================================================
    private void CacheArtwork(Node node)
    {
        if (node is Sprite2D sprite) _blocked.Add(WorldRect(sprite, sprite.GetRect()).Grow(5));
        if (node is Polygon2D polygon && polygon.Polygon.Length > 0)
        {
            Rect2 rect = new(polygon.Polygon[0], Vector2.Zero);
            foreach (Vector2 point in polygon.Polygon) rect = rect.Expand(point);
            _blocked.Add(WorldRect(polygon, rect).Grow(5));
        }
        foreach (Node child in node.GetChildren()) CacheArtwork(child);
    }

    // Four transformed corners preserve scaled/mirrored prop bounds without texture pixel reads.
    // =========================================================
    private static Rect2 WorldRect(Node2D node, Rect2 local)
    {
        Rect2 result = new(node.ToGlobal(local.Position), Vector2.Zero);
        result = result.Expand(node.ToGlobal(new Vector2(local.End.X, local.Position.Y)));
        result = result.Expand(node.ToGlobal(local.End));
        return result.Expand(node.ToGlobal(new Vector2(local.Position.X, local.End.Y)));
    }
    #endregion

    #region Simulation and drawing
    // Bounded attempts and a reusable pool keep dense forests and frame stalls inexpensive.
    // =========================================================
    public override void _Process(double delta)
    {
        for (int i = 0; i < _pool.Length; i++)
            if (_pool[i].Active) { _pool[i].Age += (float)delta; if (_pool[i].Age >= 0.28f) _pool[i].Active = false; }
        if (Weather.Config.Splashes && Weather.Rain > 0.001f)
        {
            _budget = Mathf.Min(8f, _budget + (float)delta * Mathf.Clamp(Weather.Config.SplashesPerSecond, 0f, 200f) * Weather.Rain);
            Rect2 area = Weather.VisibleWorldRect().Intersection(_world);
            Rect2 player = _player == null ? new Rect2() : WorldRect(_player, _player.GetRect()).Grow(5);
            while (_budget >= 1f && area.HasArea())
            {
                _budget -= 1f;
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    Vector2 p = area.Position + new Vector2(_random.Randf(), _random.Randf()) * area.Size;
                    if (!ClearGround(p, player)) continue;
                    _pool[_next] = new Splash { Position = p, Active = true };
                    _next = (_next + 1) % _pool.Length;
                    break;
                }
            }
        }
        else _budget = 0;
        QueueRedraw();
    }

    // Reject full prop artwork bounds, not only their small ground colliders.
    // =========================================================
    private bool ClearGround(Vector2 point, Rect2 player)
    {
        if (player.HasPoint(point)) return false;
        foreach (Rect2 rect in _blocked) if (rect.HasPoint(point)) return false;
        return true;
    }

    // These are ground-level arcs and tiny upward droplets, never surface hits on scenery.
    // =========================================================
    public override void _Draw()
    {
        if (!Weather.Config.Splashes) return;
        foreach (Splash splash in _pool)
        {
            if (!splash.Active) continue;
            float t = splash.Age / 0.28f, radius = 0.5f + t * 2.5f;
            var colour = new Color(0.66f, 0.76f, 0.80f, (1f - t) * Weather.Rain * 0.45f);
            DrawSetTransform(splash.Position, 0, new Vector2(1, 0.4f));
            DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 10, colour, 0.55f, true);
            DrawSetTransform(splash.Position);
            float rise = Mathf.Sin(t * Mathf.Pi) * 3f;
            DrawLine(new Vector2(-radius, -rise), new Vector2(-radius - 0.4f, -rise - 0.9f), colour, 0.5f, true);
            DrawLine(new Vector2(radius, -rise), new Vector2(radius + 0.4f, -rise - 0.9f), colour, 0.5f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }
    #endregion
}
