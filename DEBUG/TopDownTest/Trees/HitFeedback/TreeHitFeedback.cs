// Optional receiver: trunk-anchored visual wobble and a bounded, tree-local leaf burst.
using Godot;

public partial class TreeHitFeedback : Node2D, IWeaponHitTarget
{
    [Export] public TreeHitMasterConfig Config { get; set; }
    public int ReceivedHits { get; private set; }
    public Vector2 HitAnchor => ((Node2D)GetParent()).GlobalPosition;
    private Sprite2D _sprite;
    private CollisionPolygon2D _footprint;
    private Transform2D _original;
    private DebugWeather _weather;
    private float _age, _push;
    private int _next;
    private struct Leaf { public Vector2 Position, Velocity; public float Age, Life, Phase; }
    private Leaf[] _leaves;
    private readonly RandomNumberGenerator _random = new();
    private static readonly Vector2[] LeafShape = { new(-2, 0), new(0, -1), new(2, 0), new(0, 1) };

    #region Setup and hit contract
    // Register each authored tree automatically, including future variants using this helper.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new TreeHitMasterConfig();
        _sprite = GetParent().GetNode<Sprite2D>("Sprite");
        _footprint = GetParent().GetNode<CollisionPolygon2D>("TrunkFootprint");
        _original = _sprite.Transform;
        _weather = GetNodeOrNull<DebugWeather>("../../../Weather");
        _random.Seed = (ulong)GetInstanceId();
        AddToGroup("debug_weapon_hit_targets"); SetProcess(false);
        ProcessPriority = -1; // Let DayNight project the updated sprite transform.
    }

    // Measure actual transformed trunk polygon edges, including root scale and mirrored footprints.
    // =========================================================
    public Vector2 ClosestHitPoint(Vector2 origin)
    {
        Vector2 closest = HitAnchor; float best = float.PositiveInfinity;
        Vector2[] points = _footprint.Polygon;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p = Geometry2D.GetClosestPointToSegment(origin, _footprint.ToGlobal(points[i]), _footprint.ToGlobal(points[(i + 1) % points.Length]));
            float distance = origin.DistanceSquaredTo(p);
            if (distance < best) { best = distance; closest = p; }
        }
        return closest;
    }

    // Repeated hits restart a bounded wobble and reuse 24 leaf slots; no new particle nodes.
    // =========================================================
    public void ReceiveHit(Vector2 direction)
    {
        ReceivedHits++; _age = 0f; _push = direction.X < 0f ? -1f : 1f;
        _leaves ??= new Leaf[24];
        Rect2 art = _sprite.GetRect();
        for (int i = 0; i < Mathf.Clamp(Config.LeavesPerHit, 0, _leaves.Length); i++)
        {
            Vector2 pixel = art.Position + art.Size * new Vector2(_random.RandfRange(0.20f, 0.80f), _random.RandfRange(0.20f, 0.58f));
            _leaves[_next] = new Leaf {
                Position = _original * pixel, Velocity = new Vector2(_random.RandfRange(-12f, 12f) + direction.X * 5f, _random.RandfRange(18f, 29f)),
                Age = 0f, Life = _random.RandfRange(2.4f, 3.8f), Phase = _random.RandfRange(0f, Mathf.Tau)
            };
            _next = (_next + 1) % _leaves.Length;
        }
        SetProcess(true);
    }
    #endregion

    #region Wobble and leaves
    // Animate art around its ground anchor without rotating the StaticBody or collider.
    // =========================================================
    public override void _Process(double delta)
    {
        float dt = (float)delta; _age += dt;
        float progress = Mathf.Clamp(_age / Mathf.Max(0.1f, Config.WobbleSeconds), 0f, 1f);
        float angle = Mathf.Sin(progress * Mathf.Tau * 2f) * (1f - progress) * (1f - progress) * Mathf.DegToRad(Config.WobbleDegrees) * _push;
        _sprite.Transform = new Transform2D(angle, Vector2.Zero) * _original;
        bool alive = false;
        if (_leaves != null)
            for (int i = 0; i < _leaves.Length; i++)
            {
                ref Leaf leaf = ref _leaves[i];
                if (leaf.Life <= 0f || leaf.Age >= leaf.Life) continue;
                leaf.Age += dt;
                float wind = GodotObject.IsInstanceValid(_weather) ? _weather.WindStrength * 10f : 0f;
                leaf.Position += (leaf.Velocity + new Vector2(wind + Mathf.Sin(leaf.Age * 5f + leaf.Phase) * 8f, 0)) * dt;
                alive = true;
            }
        QueueRedraw();
        if (progress >= 1f && !alive) { _sprite.Transform = _original; SetProcess(false); }
    }

    // Only this tree emits these transient leaves; ambient wind leaves remain independent.
    // =========================================================
    public override void _Draw()
    {
        if (_leaves == null) return;
        foreach (Leaf leaf in _leaves)
        {
            if (leaf.Life <= 0f || leaf.Age >= leaf.Life) continue;
            float fade = Mathf.Clamp((leaf.Life - leaf.Age) / 0.7f, 0f, 1f);
            DrawSetTransform(leaf.Position, leaf.Phase + leaf.Age * 3f, new Vector2(1f, 0.4f + Mathf.Abs(Mathf.Sin(leaf.Age * 4f)) * 0.6f));
            DrawColoredPolygon(LeafShape, new Color(Config.LeafColour, fade));
        }
        DrawSetTransform(Vector2.Zero);
    }

    // Runtime removal leaves the original art transform intact.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_sprite)) _sprite.Transform = _original;
    }
    #endregion
}
