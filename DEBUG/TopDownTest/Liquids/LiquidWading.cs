// Generic player response to a liquid definition: visual dipping, waterline clipping and movement scale.
using Godot;

public partial class LiquidWading : Node2D
{
    public LiquidRegion Region { get; set; }
    public float Immersion { get; private set; }
    private TestPlayerMovement _player;
    private float _target, _time;

    #region Setup and response
    // Cache only the demo player, leaving colliders and camera as independent children.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = 2; ProcessPhysicsPriority = -10;
        _player = GetNode<TestPlayerMovement>("../../Actors/Player");
        PlayerSurfaceVisual.Ensure(_player).Liquid = this;
        ZIndex = -4;
    }

    // The same signed shoreline distance controls traversal and visual depth.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        float distance = Region.Active ? Region.SignedDistance(_player.GlobalPosition) : -1f;
        _target = distance <= 0 ? 0f : Mathf.Clamp(distance / Mathf.Max(10f, Region.Definition.DepthDistance), 0f, 1f);
        _player.SurfaceSpeedMultiplier = Mathf.Lerp(1f, Region.Definition.MovementMultiplier, _target);
    }

    // Animate immersion and rings; Surfaces/PlayerSurfaceVisual applies the shared sprite response.
    // =========================================================
    public override void _Process(double delta)
    {
        _time += (float)delta;
        Immersion = Region.Active ? Mathf.MoveToward(Immersion, _target, (float)delta * 3f) : 0f;
        GlobalPosition = _player.GlobalPosition;
        QueueRedraw();
    }

    // A few short surface rings provide a readable waterline without spawning particle nodes.
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

    // Release liquid movement; the shared visual helper restores snow/dry artwork independently.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_player)) _player.SurfaceSpeedMultiplier = 1f;
    }
    #endregion
}
