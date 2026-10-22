// Shares one GPU snow-cap material across opted-in rock scenes in this debug test.
// Original sprite materials are restored when master snow stops or SnowTest is removed.
using Godot;
using System.Collections.Generic;

public sealed class RockSnowAdapter
{
    private readonly Dictionary<Sprite2D, Material> _original = new();
    private ShaderMaterial _shared;
    private bool _applied;

    #region Setup
    // Capture only opt-in rocks after scenery and DayNight shadow caching have completed.
    // =========================================================
    public void Attach(Node2D actors, Texture2D noise)
    {
        if (actors == null || noise == null) return;
        _shared = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://DEBUG/TopDownTest/SnowTest/RockSnow.gdshader")
        };
        _shared.SetShaderParameter("snow_noise", noise);
        foreach (Node child in actors.GetChildren())
        {
            if (child is not Node2D rock || !rock.IsInGroup("debug_snow_rocks")) continue;
            Sprite2D sprite = rock.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite == null || _original.ContainsKey(sprite)) continue;
            _original.Add(sprite, sprite.Material);
        }
    }
    #endregion

    #region Snow response
    // Switch materials only as settled coverage starts or ends; update shared uniforms.
    // =========================================================
    public void Update(float coverage, Color colour, float capDepth)
    {
        if (_shared == null) return;
        float amount = Mathf.Clamp(coverage, 0f, 1f);
        bool show = amount > 0.0001f;
        if (show != _applied)
        {
            foreach (var pair in _original)
                if (GodotObject.IsInstanceValid(pair.Key))
                    pair.Key.Material = show ? _shared : pair.Value;
            _applied = show;
        }
        if (!show) return;
        _shared.SetShaderParameter("snow_coverage", amount);
        _shared.SetShaderParameter("snow_colour", colour);
        _shared.SetShaderParameter("cap_depth", Mathf.Clamp(capDepth, 0.25f, 0.85f));
    }

    // Restore whatever material each rock had before SnowTest was enabled.
    // =========================================================
    public void Restore()
    {
        foreach (var pair in _original)
            if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.Material = pair.Value;
        _original.Clear();
        _applied = false;
    }
    #endregion
}
