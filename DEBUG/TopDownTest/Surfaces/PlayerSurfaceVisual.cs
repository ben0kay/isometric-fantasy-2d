// Single visual owner for snow and liquid immersion; body, collider and camera never move.
using Godot;

public partial class PlayerSurfaceVisual : Node
{
    public DebugSnowTest Snow { get; set; }
    public LiquidWading Liquid { get; set; }
    private Sprite2D _sprite, _sun;
    private Polygon2D _contact;
    private Vector2 _original;
    private Material _originalMaterial;
    private ShaderMaterial _material;
    private bool _attached, _contactVisible;
    private float _snowPixels;

    // Both optional experiments share one helper rather than competing over the sprite.
    // =========================================================
    public static PlayerSurfaceVisual Ensure(TestPlayerMovement player)
    {
        var helper = player.GetNodeOrNull<PlayerSurfaceVisual>("SurfaceVisual");
        if (helper != null) return helper;
        helper = new PlayerSurfaceVisual { Name = "SurfaceVisual" }; player.AddChild(helper); return helper;
    }

    // Defer material attachment until DayNight has classified the original sprite/shadow.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = 4;
        _sprite = GetParent().GetNode<Sprite2D>("Sprite");
        _contact = GetParent().GetNode<Polygon2D>("Shadow");
        _contactVisible = _contact.Visible; _original = _sprite.Position; _originalMaterial = _sprite.Material;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://DEBUG/TopDownTest/Surfaces/PlayerImmersion.gdshader") };
    }

    // Liquid takes precedence; snow only buries a few pixels and keeps the sun shadow.
    // =========================================================
    public override void _Process(double delta)
    {
        if (!_attached)
        {
            _sprite.Material = _material; _attached = true;
            _sun = GetParent().GetNodeOrNull<Sprite2D>("../../DayNight/Shadows/PlayerSunShadow");
        }
        bool wet = GodotObject.IsInstanceValid(Liquid) && Liquid.Region.Active && Liquid.Immersion > 0.001f;
        var player = (TestPlayerMovement)GetParent();
        float target = !wet && GodotObject.IsInstanceValid(Snow) ? Snow.PlayerDepthAt(player.GlobalPosition) : 0f;
        _snowPixels = Mathf.MoveToward(_snowPixels, target, (float)delta * 12f);
        float pixels = wet ? Liquid.Immersion * Liquid.Region.Definition.MaximumSubmergePixels : _snowPixels;
        _sprite.Position = _original + new Vector2(0, pixels);
        _material.SetShaderParameter("immersion", pixels > 0.01f ? 1f : 0f);
        _material.SetShaderParameter("waterline_y", player.GlobalPosition.Y - 1f);
        _contact.Visible = _contactVisible && pixels < 0.1f;
        if (wet && GodotObject.IsInstanceValid(_sun)) _sun.Visible = false;
        if (!GodotObject.IsInstanceValid(Snow) && !GodotObject.IsInstanceValid(Liquid)) QueueFree();
    }

    // Optional helpers can be removed at runtime without leaving altered player artwork.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_sprite)) { _sprite.Position = _original; _sprite.Material = _originalMaterial; }
        if (GodotObject.IsInstanceValid(_contact)) _contact.Visible = _contactVisible;
    }
}
