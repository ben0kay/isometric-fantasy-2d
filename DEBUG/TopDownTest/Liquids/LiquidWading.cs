// Generic player response to a liquid definition: visual dipping, waterline clipping and movement scale.
using Godot;

public partial class LiquidWading : Node2D
{
    public LiquidRegion Region { get; set; }
    public float Immersion { get; private set; }
    private TestPlayerMovement _player;
    private Sprite2D _sprite;
    private Polygon2D _contact;
    private Sprite2D _sunShadow;
    private bool _shadowChecked, _contactVisible, _attached;
    private Vector2 _originalPosition;
    private Material _originalMaterial;
    private ShaderMaterial _material;
    private float _target, _time;

    #region Setup and response
    // Cache only the demo player, leaving colliders and camera as independent children.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = 2; ProcessPhysicsPriority = -10;
        _player = GetNode<TestPlayerMovement>("../../Actors/Player");
        _sprite = _player.GetNode<Sprite2D>("Sprite");
        _contact = _player.GetNode<Polygon2D>("Shadow"); _contactVisible = _contact.Visible;
        _originalPosition = _sprite.Position; _originalMaterial = _sprite.Material;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://DEBUG/TopDownTest/Liquids/Shaders/WadingSprite.gdshader") };
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

    // Shadows are created later by DayNight; cache them once after scene startup is complete.
    // =========================================================
    public override void _Process(double delta)
    {
        // Attach after DayNight has classified the original player material for its sun shadow.
        if (!_attached) { _sprite.Material = _material; _attached = true; }
        if (!_shadowChecked) { _sunShadow = GetNodeOrNull<Sprite2D>("../../DayNight/Shadows/PlayerSunShadow"); _shadowChecked = true; }
        _time += (float)delta;
        Immersion = Region.Active ? Mathf.MoveToward(Immersion, _target, (float)delta * 3f) : 0f;
        _sprite.Position = _originalPosition + new Vector2(0, Immersion * Region.Definition.MaximumSubmergePixels);
        _material.SetShaderParameter("immersion", Immersion);
        _material.SetShaderParameter("waterline_y", _player.GlobalPosition.Y - 1f);
        _contact.Visible = _contactVisible && Immersion < 0.01f;
        if (_sunShadow != null && Immersion > 0.01f) _sunShadow.Visible = false;
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

    // Removing the experiment restores the player material, position, shadow and movement scale.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_sprite)) { _sprite.Position = _originalPosition; _sprite.Material = _originalMaterial; }
        if (GodotObject.IsInstanceValid(_contact)) _contact.Visible = _contactVisible;
        if (GodotObject.IsInstanceValid(_player)) _player.SurfaceSpeedMultiplier = 1f;
    }
    #endregion
}
