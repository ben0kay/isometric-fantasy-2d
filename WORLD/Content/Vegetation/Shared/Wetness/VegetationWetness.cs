// Shared wet-foliage colour shader for generic production tree species.
// Composes with the existing wind shader, then snow/fire may extend it afterward.
using Godot;
using System.Collections.Generic;

public partial class VegetationWetness : Node
{
    public WorldWeather Weather { get; set; }
    private const string Anchor = "    COLOR = pixel * sprite_tint * inside;";
    private const string Include = "res://WORLD/Content/Vegetation/Shared/Wetness/VegetationWetness.gdshaderinc";
    private readonly Dictionary<Sprite2D, Material> _original = new();
    private readonly Dictionary<Sprite2D, ShaderMaterial> _owned = new();
    private readonly List<ShaderMaterial> _materials = new();
    private readonly Dictionary<ShaderMaterial,ShaderMaterial> _sources = new();
    public float Coverage => _coverage;
    // New chunks start at the shared world wetness instead of appearing dry during an ongoing storm.
    // =========================================================
    public void StartAt(float value) { _coverage = Mathf.Clamp(value,0,1); _lastCoverage = -1; }
    private float _coverage, _lastCoverage = -1f, _lastDarkening = -1f;

    #region Attach
    // World trees opt in through their generic group/foliage profile, not their species name.
    // Call after VegetationWind.Attach and BEFORE WorldSnow creates its shared variants.
    // =========================================================
    public void Attach(Node2D actors)
    {
        var shaders = new Dictionary<Shader, Shader>();
        var variants = new Dictionary<(ShaderMaterial, int), ShaderMaterial>();
        foreach (Node tree in actors.GetChildren())
        {
            if (!tree.IsInGroup("world_trees")) continue;
            Sprite2D sprite = tree.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Material is not ShaderMaterial source || source.Shader == null) continue;
            string code = source.Shader.Code;
            if (!code.Contains(Anchor) || !code.Contains("shader_type canvas_item;")) continue;
            if (!shaders.TryGetValue(source.Shader, out Shader shader))
            {
                code = code.Replace("shader_type canvas_item;",
                    "shader_type canvas_item;\n#include \"" + Include + "\"");
                code = code.Replace(Anchor,
                    "    if (!shadow_only) pixel.rgb = wet_foliage_colour(pixel.rgb, UV);\n" + Anchor);
                shader = new Shader { Code = code };
                shaders.Add(source.Shader, shader);
            }
            int profile = sprite.HasMeta("snow_foliage_profile")
                ? (int)sprite.GetMeta("snow_foliage_profile").AsInt32() : 0;
            if (!variants.TryGetValue((source, profile), out ShaderMaterial material))
            {
                material = (ShaderMaterial)source.Duplicate();
                material.Shader = shader;
                material.SetShaderParameter("wet_foliage_profile", (float)profile);
                material.SetShaderParameter("wet_coverage", 0f);
                variants.Add((source, profile), material);
                _materials.Add(material); _sources.Add(material,source);
            }
            _original.Add(sprite, source);
            _owned.Add(sprite, material);
            sprite.Material = material;
        }
        ProcessPriority = -5;
    }
    #endregion

    #region One shared rain response
    // No per-tree frame processing; only distinct shader materials receive changing parameters.
    // =========================================================
    public override void _Process(double delta)
    {
        if (Weather?.Config == null) return;
        foreach (var pair in _sources) pair.Key.SetShaderParameter("wind_multiplier",pair.Value.GetShaderParameter("wind_multiplier"));
        WeatherProfile settings = Weather.Config;
        float target = settings.WetFoliage ? Weather.Rain : 0f;
        float seconds = target > _coverage
            ? Mathf.Max(0.1f, settings.WetnessBuildSeconds)
            : Mathf.Max(0.1f, settings.WetnessDrySeconds);
        _coverage = Mathf.MoveToward(_coverage, Mathf.Clamp(target, 0f, 1f), (float)delta / seconds);
        float darkening = Mathf.Clamp(settings.WetFoliageDarkening, 0f, 0.35f);
        if (Mathf.Abs(_coverage - _lastCoverage) < 0.001f
            && Mathf.IsEqualApprox(darkening, _lastDarkening)) return;
        _lastCoverage = _coverage; _lastDarkening = darkening;
        foreach (ShaderMaterial material in _materials)
        {
            material.SetShaderParameter("wet_coverage", _coverage);
            material.SetShaderParameter("wet_darkening", darkening);
        }
    }
    #endregion

    #region Cleanup
    // Restore live wind variants only when this helper still owns the material slot.
    // =========================================================
    public override void _ExitTree()
    {
        foreach (var pair in _original)
            if (GodotObject.IsInstanceValid(pair.Key) && pair.Key.Material == _owned[pair.Key])
                pair.Key.Material = pair.Value;
        _original.Clear(); _owned.Clear(); _materials.Clear(); _sources.Clear();
    }
    #endregion
}
