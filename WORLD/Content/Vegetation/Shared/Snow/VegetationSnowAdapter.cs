// Shared snow variants preserve production vegetation materials and the common wind cache.
using Godot;
using System.Collections.Generic;

public sealed class VegetationSnowAdapter
{
    private readonly Dictionary<Sprite2D, Material> _original = new();
    private readonly List<(ShaderMaterial Source, ShaderMaterial Snow, bool Wet)> _materials = new();
    private readonly Dictionary<Sprite2D, Material> _adapted = new();
    private bool _applied;

    // Shared normal/mirrored variants work for tree roots or direct grass sprites using the sway shader.
    // =========================================================
    public void Attach(Node2D actors, Texture2D noise)
    {
        const string output = "    COLOR = pixel * sprite_tint * inside;";
        var variants = new Dictionary<(ShaderMaterial, bool, int), ShaderMaterial>();
        var shaders = new Dictionary<Shader, Shader>();
        foreach (Node child in actors.GetChildren())
        {
            if (!child.HasMeta("environment_snow") || child.GetMeta("environment_snow").AsInt32() != 1) continue;
            bool grass = child.GetMeta("environment_grass").AsBool();
            Sprite2D sprite = child.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Material is not ShaderMaterial material) continue;
            Shader original = material.Shader;
            if (!original.Code.Contains(output)) { GD.PushWarning("Snow adapter skipped an unsupported tree shader."); continue; }
            if (!shaders.TryGetValue(original, out Shader extended))
            {
                string code = original.Code.Replace("shader_type canvas_item;", "shader_type canvas_item;\n#include \"res://WORLD/Content/Vegetation/Shared/Snow/VegetationSnow.gdshaderinc\"");
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
                variants.Add(key, snow); _materials.Add((material, snow,
                    original.Code.Contains("VegetationWetness.gdshaderinc")));
            }
            _original.Add(sprite, material);
            _adapted.Add(sprite, snow);
        }
    }

    // Copy the existing wind cache to shared snow variants, never update every individual tree.
    // =========================================================
    public void Update(float coverage, Color colour)
    {
        bool show = coverage > 0.0001f;
        if (show != _applied)
        {
            foreach (var pair in _original)
                if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.Material = show ? _adapted[pair.Key] : pair.Value;
            _applied = show;
        }
        foreach (var pair in _materials)
        {
            pair.Snow.SetShaderParameter("wind_multiplier", pair.Source.GetShaderParameter("wind_multiplier"));
            if (pair.Wet)
            {
                // This snow material was cloned from the production wet/wind variant.
                // Preserve the ongoing rain/dry changes beneath its snow colour overlay.
                pair.Snow.SetShaderParameter("wet_coverage", pair.Source.GetShaderParameter("wet_coverage"));
                pair.Snow.SetShaderParameter("wet_darkening", pair.Source.GetShaderParameter("wet_darkening"));
            }
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
        }
        _original.Clear(); _adapted.Clear(); _materials.Clear(); _applied = false;
    }
}
