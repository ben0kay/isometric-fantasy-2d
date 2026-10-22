// Editable biome identity, ground palette and scenery permissions for this disposable test.
using Godot;

[GlobalClass]
public partial class BiomeDefinition : Resource
{
    [Export] public string Id { get; set; } = "oak_forest";
    [Export] public string DisplayName { get; set; } = "Oak Forest";
    [ExportGroup("Ground palette")]
    [Export] public Color BaseColour { get; set; } = new(0.2196f, 0.2745f, 0.2078f);
    [Export] public Color ShadeColour { get; set; } = new(0.18f, 0.245f, 0.18f);
    [Export] public Color FreshColour { get; set; } = new(0.28f, 0.335f, 0.23f);
    [Export] public Color DryColour { get; set; } = new(0.32f, 0.32f, 0.22f);
    [ExportGroup("Tree population")]
    [Export] public PackedScene TreeScene { get; set; }
    [Export(PropertyHint.Range, "0,150,1")] public int TreeCount { get; set; } = 0;

    [ExportGroup("Scenery permissions")]
    [Export] public bool AllowTrees { get; set; } = true;
    [Export] public bool AllowGrassTufts { get; set; } = true;
    [Export] public bool AllowRocks { get; set; } = true;
}
