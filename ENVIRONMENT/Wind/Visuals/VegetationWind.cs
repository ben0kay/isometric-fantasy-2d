// One shared material per source/orientation; no per-tree or per-tuft processors.
using Godot;
using System.Collections.Generic;
public partial class VegetationWind : Node
{
    public WindController Wind { get; set; }
    private readonly Dictionary<Sprite2D, Material> _original = new();
    private readonly List<ShaderMaterial> _materials = new();
    // Clone authored resources once, so runtime wind changes never modify saved scene materials.
    // =========================================================
    public void Attach(Node2D actors)
    {
        var variants = new Dictionary<(ShaderMaterial, bool), ShaderMaterial>();
        foreach (Node child in actors.GetChildren())
        {
            Sprite2D sprite = child.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Material is not ShaderMaterial source || !source.Shader.Code.Contains("wind_multiplier")) continue;
            if (!variants.TryGetValue((source, sprite.FlipH), out ShaderMaterial material))
            {
                material = (ShaderMaterial)source.Duplicate();
                material.SetShaderParameter("wind_orientation", sprite.FlipH ? -1f : 1f);
                variants.Add((source, sprite.FlipH), material); _materials.Add(material);
            }
            _original.Add(sprite, source); sprite.Material = material;
        }
        ProcessPriority = -15; UpdateWind();
    }
    // Update only shared variants after the single wind source advances.
    // =========================================================
    public override void _Process(double delta) => UpdateWind();
    // Initialize and tick using the same shared wind value as shadows and precipitation.
    // =========================================================
    private void UpdateWind()
    {
        foreach (ShaderMaterial material in _materials) material.SetShaderParameter("wind_multiplier", Wind.Strength);
    }
    // Removing the environment restores authored materials, including mirrored variants.
    // =========================================================
    public override void _ExitTree()
    {
        foreach (var pair in _original)
            if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.Material = pair.Value;
    }
}
