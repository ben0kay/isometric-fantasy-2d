// Temporary snow materials preserve original tree resources and the day/night wind cache.
using Godot;
using System.Collections.Generic;

public sealed class TreeSnowAdapter
{
    private readonly Dictionary<Sprite2D, Material> _original = new();
    private readonly List<(ShaderMaterial Source, ShaderMaterial Snow, bool Grass)> _materials = new();

    // Shared normal/mirrored variants work for tree roots or direct grass sprites using the sway shader.
    // =========================================================
    public void Attach(Node2D actors, Texture2D noise, bool grass = false)
    {
        const string output = "    COLOR = pixel * sprite_tint * inside;";
        var variants = new Dictionary<(ShaderMaterial, bool, int), ShaderMaterial>();
        var shaders = new Dictionary<Shader, Shader>();
        foreach (Node child in actors.GetChildren())
        {
            if (!grass && !child.Name.ToString().StartsWith("Tree")) continue;
            Sprite2D sprite = grass ? child as Sprite2D : child.GetNodeOrNull<Sprite2D>("Sprite");
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
            // Per-sprite foliage profiles: 0=green leaves, 1=pink blossoms.
            // This is optional sprite metadata, not a special-case tree filename.
            int foliage = sprite.HasMeta("snow_foliage_profile")
                ? (int)sprite.GetMeta("snow_foliage_profile").AsInt32() : 0;
            var key = (material, sprite.FlipH, foliage);
            if (!variants.TryGetValue(key, out ShaderMaterial snow))
            {
                snow = (ShaderMaterial)material.Duplicate(); snow.Shader = extended;
                snow.SetShaderParameter("wind_orientation", sprite.FlipH ? -1f : 1f);
                snow.SetShaderParameter("snow_noise", noise);
                snow.SetShaderParameter("snow_foliage_profile", (float)foliage);
                snow.SetShaderParameter("snow_crown_range", grass ? new Vector2(0.82f, 1f) : new Vector2(0.60f, 0.77f));
                variants.Add(key, snow); _materials.Add((material, snow, grass));
            }
            _original.Add(sprite, material);
            sprite.SetInstanceShaderParameter("wind_orientation", default);
            sprite.Material = snow;
        }
    }

    // Copy the existing wind cache to shared snow variants, never update every individual tree.
    // =========================================================
    public void Update(float coverage, Color colour, bool grassSnow = true)
    {
        foreach (var pair in _materials)
        {
            pair.Snow.SetShaderParameter("wind_multiplier", pair.Source.GetShaderParameter("wind_multiplier"));
            pair.Snow.SetShaderParameter("snow_coverage", pair.Grass && !grassSnow ? 0f : coverage);
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
