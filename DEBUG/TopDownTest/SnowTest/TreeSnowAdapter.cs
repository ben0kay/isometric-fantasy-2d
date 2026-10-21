// Temporary snow materials preserve original tree resources and the day/night wind cache.
using Godot;
using System.Collections.Generic;

public sealed class TreeSnowAdapter
{
    private readonly Dictionary<Sprite2D, Material> _original = new();
    private readonly List<(ShaderMaterial Source, ShaderMaterial Snow)> _materials = new();

    // Two shared variants cover normal/mirrored trees without adding per-instance snow uniforms.
    // =========================================================
    public void Attach(Node2D actors, Texture2D noise)
    {
        const string output = "    COLOR = pixel * sprite_tint * inside;";
        var variants = new Dictionary<(ShaderMaterial, bool), ShaderMaterial>();
        var shaders = new Dictionary<Shader, Shader>();
        foreach (Node child in actors.GetChildren())
        {
            if (!child.Name.ToString().StartsWith("Tree")) continue;
            Sprite2D sprite = child.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Material is not ShaderMaterial material) continue;
            Shader original = material.Shader;
            if (!original.Code.Contains(output)) { GD.PushWarning("Snow preview skipped an unsupported tree shader."); continue; }
            if (!shaders.TryGetValue(original, out Shader extended))
            {
                string code = original.Code.Replace("shader_type canvas_item;", "shader_type canvas_item;\n#include \"res://DEBUG/TopDownTest/SnowTest/TreeSnow.gdshaderinc\"");
                code = code.Replace("instance uniform float wind_orientation", "uniform float wind_orientation");
                code = code.Replace(output, "    if (!shadow_only) pixel.rgb = tree_snow_colour(pixel.rgb, sample_uv);\n" + output);
                extended = new Shader { Code = code }; shaders.Add(original, extended);
            }
            var key = (material, sprite.FlipH);
            if (!variants.TryGetValue(key, out ShaderMaterial snow))
            {
                snow = (ShaderMaterial)material.Duplicate(); snow.Shader = extended;
                snow.SetShaderParameter("wind_orientation", sprite.FlipH ? -1f : 1f);
                snow.SetShaderParameter("snow_noise", noise);
                variants.Add(key, snow); _materials.Add((material, snow));
            }
            _original.Add(sprite, material);
            sprite.SetInstanceShaderParameter("wind_orientation", default);
            sprite.Material = snow;
        }
    }

    // Copy the existing wind cache to shared snow variants, never update every individual tree.
    // =========================================================
    public void Update(float coverage, Color colour)
    {
        foreach (var pair in _materials)
        {
            pair.Snow.SetShaderParameter("wind_multiplier", pair.Source.GetShaderParameter("wind_multiplier"));
            pair.Snow.SetShaderParameter("snow_coverage", coverage);
            pair.Snow.SetShaderParameter("snow_colour", colour);
        }
    }

    // Removal restores original material identities and mirrored-wind instance parameters.
    // =========================================================
    public void Restore()
    {
        foreach (var pair in _original)
        {
            if (!GodotObject.IsInstanceValid(pair.Key)) continue;
            pair.Key.Material = pair.Value;
            pair.Key.SetInstanceShaderParameter("wind_orientation", pair.Key.FlipH ? -1f : 1f);
        }
        _original.Clear(); _materials.Clear();
    }
}
