// Reusable water wading, using the existing production player/snow visual owner.
using Godot;
using System;
public partial class WorldLiquidWading : Node2D
{
    public WorldLiquidRegion Region { get; set; }
    public Func<Vector2,WorldLiquidRegion> RegionAt { get; set; }
    private WorldLiquidRegion _current;
    private Color _highlight;
    public PlayerMovement Player { get; set; }
    public float Immersion { get; private set; }
    private float _target, _time;

    // One shared sprite owner prevents water and snow from replacing each other's material.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = 2; ProcessPhysicsPriority = -10;
        PlayerSnowVisual.Ensure(Player).Liquid = this;
        ZIndex = -4; SetProcess(false);
    }

    // The cached region polygon also drives movement slowdown.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldLiquidWading.PhysicsProcess");
        _current=RegionAt?.Invoke(Player.GlobalPosition) ?? Region;
        bool active=GodotObject.IsInstanceValid(_current) && _current.Active;
        float distance = active && _current.SurfaceBounds.HasPoint(Player.GlobalPosition) ? _current.SignedDistance(Player.GlobalPosition) : -1f;
        _target = distance <= 0 ? 0f : Mathf.Clamp(distance / Mathf.Max(10f, _current.Definition.DepthDistance), 0f, 1f);
        Player.SurfaceSpeedMultiplier = Mathf.Lerp(1f, active ? _current.Definition.MovementMultiplier : 1f, _target);
        if (active) _highlight=_current.Definition.HighlightColour;
        if (_target>0 || Immersion>0) SetProcess(true);
    }

    // Draw a pair of small waterline rings without creating extra particle nodes.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldLiquidWading.Process");
        _time += (float)delta;
        Immersion = Mathf.MoveToward(Immersion, _target, (float)delta * 3f);
        GlobalPosition = Player.GlobalPosition;
        QueueRedraw();
        if (Immersion==0 && _target==0) SetProcess(false);
    }

    // Reuse the proven DEBUG wading-ring appearance.
    // =========================================================
    public override void _Draw()
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldLiquidWading.Draw");
        if (Immersion <= 0.01f) return;
        DrawSetTransform(new Vector2(0, -1), 0, new Vector2(1f, 0.35f));
        for (int i = 0; i < 2; i++)
        {
            float phase = Mathf.PosMod(_time * 0.7f + i * 0.5f, 1f);
            DrawArc(Vector2.Zero, 10f + phase * 8f, 0, Mathf.Tau, 24,
                new Color(_highlight, (1f - phase) * Immersion * 0.32f), 0.7f, true);
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
