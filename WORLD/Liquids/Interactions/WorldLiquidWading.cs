// Reusable water wading, using the existing production player/snow visual owner.
using Godot;
public partial class WorldLiquidWading : Node2D
{
    public WorldLiquidRegion Region { get; set; }
    public PlayerMovement Player { get; set; }
    public float Immersion { get; private set; }
    private float _target, _time;

    // One shared sprite owner prevents water and snow from replacing each other's material.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = 2; ProcessPhysicsPriority = -10;
        PlayerSnowVisual.Ensure(Player).Liquid = this;
        ZIndex = -4;
    }

    // The cached region polygon also drives movement slowdown.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        float distance = Region.Active ? Region.SignedDistance(Player.GlobalPosition) : -1f;
        _target = distance <= 0 ? 0f : Mathf.Clamp(distance / Mathf.Max(10f, Region.Definition.DepthDistance), 0f, 1f);
        Player.SurfaceSpeedMultiplier = Mathf.Lerp(1f, Region.Definition.MovementMultiplier, _target);
    }

    // Draw a pair of small waterline rings without creating extra particle nodes.
    // =========================================================
    public override void _Process(double delta)
    {
        _time += (float)delta;
        Immersion = Region.Active ? Mathf.MoveToward(Immersion, _target, (float)delta * 3f) : 0f;
        GlobalPosition = Player.GlobalPosition;
        QueueRedraw();
    }

    // Reuse the proven DEBUG wading-ring appearance.
    // =========================================================
    public override void _Draw()
    {
        if (Immersion <= 0.01f) return;
        DrawSetTransform(new Vector2(0, -1), 0, new Vector2(1f, 0.35f));
        for (int i = 0; i < 2; i++)
        {
            float phase = Mathf.PosMod(_time * 0.7f + i * 0.5f, 1f);
            DrawArc(Vector2.Zero, 10f + phase * 8f, 0, Mathf.Tau, 24,
                new Color(Region.Definition.HighlightColour, (1f - phase) * Immersion * 0.32f), 0.7f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(Player)) Player.SurfaceSpeedMultiplier = 1f;
        if (GodotObject.IsInstanceValid(Player))
        {
            var visual = Player.GetNodeOrNull<PlayerSnowVisual>("SurfaceVisual");
            if (visual != null && visual.Liquid == this) visual.Liquid = null;
        }
    }
}
