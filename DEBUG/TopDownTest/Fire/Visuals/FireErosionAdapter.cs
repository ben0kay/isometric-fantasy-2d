// Adds one burn-mask function to the CURRENT snow/wind shader for an active oak.
// It never edits shared shaders/materials and restores exactly the live source material.
using Godot;
using System.Collections.Generic;

public sealed class FireErosionAdapter
{
    private const string OutputAnchor = "    COLOR = pixel * sprite_tint * inside;";
    private const string IncludePath = "res://DEBUG/TopDownTest/Fire/Visuals/FireErosion.gdshaderinc";
    private static readonly Dictionary<Shader, Shader> ExtendedShaders = new();

    private Sprite2D _sprite;
    private Texture2D _noise;
    private ShaderMaterial _source, _burning;
    private bool _hasSnow;

    public bool IsActive => _burning != null;

    #region Bind and update
    // Called once at ignition. Unsupported sprite shaders retain FIRE1's simple fade.
    // =========================================================
    public bool Attach(Sprite2D sprite, Texture2D noise)
    {
        _sprite = sprite; _noise = noise;
        return Install(sprite?.Material as ShaderMaterial);
    }

    // Compose with both the original TreeSway shader and SnowTest's extended variant.
    // =========================================================
    private bool Install(ShaderMaterial source)
    {
        if (source?.Shader == null || _noise == null || _sprite == null) return false;
        Shader shader = source.Shader;
        string code = shader.Code;
        if (!code.Contains(OutputAnchor) || !code.Contains("shader_type canvas_item;"))
        {
            GD.PushWarning("FIRE2: unsupported tree shader, using FIRE1 crossfade for this oak.");
            return false;
        }
        if (!ExtendedShaders.TryGetValue(shader, out Shader extended))
        {
            code = code.Replace("shader_type canvas_item;",
                "shader_type canvas_item;\n#include \"" + IncludePath + "\"");
            code = code.Replace("instance uniform float wind_orientation", "uniform float wind_orientation");
            code = code.Replace(OutputAnchor,
                "    pixel = fire_erode_pixel(pixel, sample_uv, UV);\n" + OutputAnchor);
            extended = new Shader { Code = code };
            ExtendedShaders.Add(shader, extended);
        }
        _source = source;
        _hasSnow = shader.Code.Contains("TreeSnow.gdshaderinc");
        _burning = (ShaderMaterial)source.Duplicate();
        _burning.Shader = extended;
        _burning.SetShaderParameter("fire_burn_noise", _noise);
        _burning.SetShaderParameter("fire_burn_progress", 0f);
        _burning.SetShaderParameter("wind_orientation", _sprite.FlipH ? -1f : 1f);
        _sprite.Material = _burning;
        return true;
    }

    // Mirror active wind/snow parameters without modifying their shared source materials.
    // This also recovers if removing SnowTest changes this sprite's material mid-burn.
    // =========================================================
    public bool Update(float progress, float wind, Color charColour)
    {
        if (!GodotObject.IsInstanceValid(_sprite)) return false;
        if (_sprite.Material != _burning && !Install(_sprite.Material as ShaderMaterial)) return false;
        _burning.SetShaderParameter("fire_burn_progress", Mathf.Clamp(progress, 0f, 1f));
        _burning.SetShaderParameter("fire_char_colour", charColour);
        _burning.SetShaderParameter("wind_multiplier", wind);
        if (_hasSnow)
        {
            _burning.SetShaderParameter("snow_coverage", _source.GetShaderParameter("snow_coverage"));
            _burning.SetShaderParameter("snow_colour", _source.GetShaderParameter("snow_colour"));
        }
        return true;
    }
    #endregion

    #region Cleanup
    // Never restore a stale material if another adapter replaced this sprite meanwhile.
    // =========================================================
    public void Restore()
    {
        if (GodotObject.IsInstanceValid(_sprite) && _sprite.Material == _burning)
            _sprite.Material = _source;
        _source = null; _burning = null; _sprite = null; _noise = null;
    }
    #endregion
}
