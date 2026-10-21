// Builds one small shared-material gravel decal per generated rock, underneath actors/shadows.
using Godot;
using System.Collections.Generic;

public partial class DebugRockPatches : Node2D
{
    [Export] public RockPatchMasterConfig Config { get; set; }
    [Export] public ShaderMaterial PatchMaterial { get; set; }
    private readonly List<(MeshInstance2D Decal, Vector2 Size)> _patches = new();
    private ShaderMaterial _material;
    private Node2D _decals;
    private HSlider _spread;
    private bool _syncing;
    private float _lastSpread = -1f;

    #region Setup
    // Actors have completed their seeded scattering before this sibling enters Ready.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new RockPatchMasterConfig();
        _material = (ShaderMaterial)PatchMaterial.Duplicate();
        _decals = new Node2D { Name = "Decals" }; AddChild(_decals);
        var quad = new QuadMesh { Size = Vector2.One * 2f };
        var random = new RandomNumberGenerator { Seed = 73121 };
        foreach (Node child in GetNode<Node2D>("../Actors").GetChildren())
        {
            if (child is not Node2D rock || !child.Name.ToString().StartsWith("Rock")) continue;
            Sprite2D sprite = rock.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite == null) continue;
            float width = sprite.GetRect().Size.X * sprite.Scale.Abs().X * rock.Scale.Abs().X;
            Vector2 radius = new(width * 0.90f, width * 0.40f);
            var decal = new MeshInstance2D
            {
                Name = rock.Name + "Gravel", Mesh = quad, Material = _material,
                Position = rock.Position + new Vector2(0, -8f * rock.Scale.Abs().Y),
                Rotation = random.RandfRange(-0.16f, 0.16f)
            };
            _decals.AddChild(decal);
            _patches.Add((decal, radius));
        }
        BuildControls();
    }

    // Gravel comparisons remain independent of the grass toggle and weather controls.
    // =========================================================
    private void BuildControls()
    {
        var ui = new CanvasLayer { Name = "UI", Layer = 20 }; AddChild(ui);
        var panel = new PanelContainer { OffsetLeft = 16, OffsetTop = 605, OffsetRight = 336, OffsetBottom = 725 };
        ui.AddChild(panel);
        var controls = new VBoxContainer(); panel.AddChild(controls);
        var toggle = new CheckButton { Text = "Rock ground gravel", ButtonPressed = Config.Enabled, FocusMode = Control.FocusModeEnum.None };
        toggle.Toggled += value => Config.Enabled = value; controls.AddChild(toggle);
        controls.AddChild(new Label { Text = "Gravel spread" });
        _spread = new HSlider { MinValue = 0.5, MaxValue = 2, Step = 0.05, Value = Config.Spread, FocusMode = Control.FocusModeEnum.None };
        _spread.ValueChanged += value => { if (!_syncing) Config.Spread = (float)value; }; controls.AddChild(_spread);
    }
    #endregion

    #region Live settings
    // Change shared uniforms; only resize cached decals when the spread setting actually changes.
    // =========================================================
    public override void _Process(double delta)
    {
        _decals.Visible = Config.Enabled;
        _material.SetShaderParameter("opacity", Mathf.Clamp(Config.Opacity, 0f, 1f));
        _material.SetShaderParameter("dark_gravel", Config.DarkGravel);
        _material.SetShaderParameter("light_gravel", Config.LightGravel);
        float spread = Mathf.Clamp(Config.Spread, 0.5f, 2f);
        if (!Mathf.IsEqualApprox(_lastSpread, spread))
        {
            foreach (var patch in _patches) patch.Decal.Scale = patch.Size * spread;
            _lastSpread = spread;
        }
        _syncing = true; _spread.Value = spread; _syncing = false;
    }
    #endregion
}
