// Optional receiver: trunk-anchored visual wobble and a bounded, tree-local leaf burst.
using Godot;

public partial class TreeHitResponse : Node2D, IToolHitTarget, IHarvestHitTarget
{
    private bool _enabled=true;
    [Export] public bool Enabled
    {
        get => _enabled;
        set { if (_enabled==value) return; _enabled=value; if (!value) StopVisuals();
            if (IsNodeReady()) { if (value) AddToGroup("world_tool_targets"); else RemoveFromGroup("world_tool_targets"); } }
    }
    [Export] public TreeHitConfig Config { get; set; }
    public Color FoliageColour { get; set; } = new(0.58f, 0.57f, 0.24f);
    public bool FoliageEnabled { get; set; } = true;
    public bool HealthyLeaves { get; private set; } = true;
    public event System.Action VisualTransformChanged;
    public int ReceivedHits { get; private set; }
    public Harvestable Harvesting { get; set; }
    public Vector2 HitAnchor => ((Node2D)GetParent()).GlobalPosition;
    private Sprite2D _sprite;
    private CollisionPolygon2D _footprint;
    private Transform2D _original;
    private WorldWeather _weather;
    private float _age, _push;
    private bool _wobbling;
    private ParticleEmitter _leaves;
    private static readonly ParticleProfile DefaultLeaves = new();
    private readonly RandomNumberGenerator _random = new();
    public int ActiveParticleCount => _leaves?.ActiveCount ?? 0;
    public bool IsWobbling => _wobbling;

    #region Setup and hit contract
    // Register each authored tree automatically, including future variants using this helper.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new TreeHitConfig(); HealthyLeaves = FoliageEnabled;
        _sprite = GetParent().GetNode<Sprite2D>("Sprite");
        _footprint = GetParent().GetNode<CollisionPolygon2D>("TrunkFootprint");
        _original = _sprite.Transform;
        _weather = GetTree().Root.FindChild("Weather", true, false) as WorldWeather;
        _random.Seed = (ulong)GetInstanceId();
        if (Enabled) AddToGroup("world_tool_targets"); SetProcess(false);
        ProcessPriority = -1; // Let DayNight project the updated sprite transform.
    }

    // Optional lifecycle callers can suppress foliage without coupling the hit receiver to fire.
    // =========================================================
    public void SetHealthyLeaves(bool enabled)
    {
        HealthyLeaves = enabled && FoliageEnabled;
        if (!HealthyLeaves && _leaves?.Active==true) { _leaves.Clear(); QueueRedraw(); }
        if (!_wobbling && _leaves?.Active!=true) SetProcess(false);
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

    // Enriched confirmed contacts carry snapshotted tool work; legacy visual hits never count as chopping.
    // =========================================================
    public void ReceiveHarvestHit(HarvestHit hit)
    {
        if (!Enabled) return;
        if (Harvesting!=null && Harvesting.Apply(hit)==HarvestWorkResult.Completed) return;
        ReceiveHit(hit.Direction);
    }

    // Repeated hits restart a bounded wobble and reuse 24 leaf slots; no new particle nodes.
    // =========================================================
    public void ReceiveHit(Vector2 direction)
    {
        if (!Enabled) return;
        if (!direction.IsFinite()) direction=Vector2.Zero;
        ReceivedHits++; _age = 0f; _push = direction.X < 0f ? -1f : 1f; _wobbling=true;
        int count=HealthyLeaves ? Mathf.Clamp(Config.LeavesPerHit,0,24) : 0;
        if (count>0)
        {
            _leaves ??= new ParticleEmitter(24);
            Rect2 art=_sprite.GetRect();
            var region=new Rect2(art.Position+art.Size*new Vector2(0.20f,0.20f),art.Size*new Vector2(0.60f,0.38f));
            // Preserve the old tree's horizontal hit bias; generic callers can use a full directional vector.
            _leaves.EmitBurst((Config.LeafParticles ?? DefaultLeaves).Snapshot(),count,region,_original,new Vector2(direction.X,0),FoliageColour,_random);
            QueueRedraw();
        }
        SetProcess(true);
    }
    #endregion

    #region Wobble and leaves
    // Animate art around its ground anchor without rotating the StaticBody or collider.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("TreeHitResponse.Process");
        float dt=(float)delta;
        if (_wobbling)
        {
            _age+=dt;
            float duration=float.IsFinite(Config.WobbleSeconds) ? Mathf.Clamp(Config.WobbleSeconds,0.1f,10) : 0.65f;
            float degrees=float.IsFinite(Config.WobbleDegrees) ? Mathf.Clamp(Config.WobbleDegrees,0,6) : 2.8f;
            float progress=Mathf.Clamp(_age/duration,0,1);
            float angle=Mathf.Sin(progress*Mathf.Tau*2f)*(1-progress)*(1-progress)*Mathf.DegToRad(degrees)*_push;
            _sprite.Transform=progress>=1 ? _original : new Transform2D(angle,Vector2.Zero)*_original;
            VisualTransformChanged?.Invoke();
            if (progress>=1) _wobbling=false;
        }
        if (_leaves?.Active==true)
        {
            float wind=GodotObject.IsInstanceValid(_weather) ? _weather.WindStrength*10f : 0;
            _leaves.Advance(dt,new Vector2(wind,0)); QueueRedraw();
        }
        if (!_wobbling && _leaves?.Active!=true) SetProcess(false);
    }

    // Only this tree emits these transient leaves; ambient wind leaves remain independent.
    // =========================================================
    public override void _Draw()
    {
        using var frameTrace = RuntimeFrameTrace.Measure("TreeHitResponse.Draw");
        _leaves?.Draw(this);
    }

    // Disabled/chunk-retired owners cannot resume stale particles or a half-finished wobble.
    // =========================================================
    public override void _Notification(int what) { if (what==NotificationDisabled) StopVisuals(); }
    // Restore art once and clear any final canvas commands without retaining weather/tree references.
    // =========================================================
    private void StopVisuals()
    {
        _wobbling=false; _age=0;
        if (_leaves?.Active==true) { _leaves.Clear(); QueueRedraw(); }
        if (GodotObject.IsInstanceValid(_sprite) && _sprite.Transform!=_original)
        { _sprite.Transform=_original; VisualTransformChanged?.Invoke(); }
        SetProcess(false);
    }
    // Runtime removal leaves the original art transform intact.
    // =========================================================
    public override void _ExitTree() { StopVisuals(); _weather=null; }

    #endregion
}
