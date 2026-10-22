// Single player snow-visual owner; physics body, collider and camera never move.
using Godot;

public partial class PlayerSnowVisual : Node
{
    public WorldSnow Snow { get; set; }
    public LiquidWading Liquid { get; set; }
    private Sprite2D _sprite;
    private Polygon2D _contact;
    private Vector2 _original;
    private Material _originalMaterial;
    private ShaderMaterial _material;
    private bool _attached, _contactVisible;
    private float _snowPixels;

    // Ensure one visual adapter; future surface effects should extend the same ownership contract.
    // =========================================================
    public static PlayerSnowVisual Ensure(PlayerMovement player)
    {
        var helper = player.GetNodeOrNull<PlayerSnowVisual>("SurfaceVisual");
        if (helper != null) return helper;
        helper = new PlayerSnowVisual { Name = "SurfaceVisual" }; player.AddChild(helper); return helper;
    }

    // Defer material attachment until DayNight has classified the original sprite/shadow.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = 4;
        _sprite = GetParent().GetNode<Sprite2D>("Sprite");
        _contact = GetParent().GetNode<Polygon2D>("Shadow");
        _contactVisible = _contact.Visible; _original = _sprite.Position; _originalMaterial = _sprite.Material;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://ENTITIES/Player/Visuals/Surfaces/PlayerSnowClip.gdshader") };
    }

    // Snow buries only a few pixels and preserves the projected sun shadow.
    // =========================================================
    public override void _Process(double delta)
    {
        if (!_attached)
        {
            _sprite.Material = _material; _attached = true;
        }
        var player = (PlayerMovement)GetParent();
        bool wet = GodotObject.IsInstanceValid(Liquid) && Liquid.Region.Active && Liquid.Immersion > 0.001f;
        float target = !wet && GodotObject.IsInstanceValid(Snow) ? Snow.PlayerDepthAt(player.GlobalPosition) : 0f;
        _snowPixels = Mathf.MoveToward(_snowPixels, target, (float)delta * 12f);
        float pixels = wet ? Liquid.Immersion * Liquid.Region.Definition.MaximumSubmergePixels : _snowPixels;
        _sprite.Position = _original + new Vector2(0, pixels);
        _material.SetShaderParameter("burial", pixels > 0.01f ? 1f : 0f);
        _material.SetShaderParameter("groundline_y", player.GlobalPosition.Y - 1f);
        _contact.Visible = _contactVisible && pixels < 0.1f;
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
