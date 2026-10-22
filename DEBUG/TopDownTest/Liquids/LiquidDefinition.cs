// Liquid type data only: the same region/renderer can display water or another liquid profile.
using Godot;

[GlobalClass]
public partial class LiquidDefinition : Resource
{
    [Export] public string Id { get; set; } = "water";
    [Export] public string DisplayName { get; set; } = "Water";
    [ExportGroup("Surface")]
    [Export] public Color ShallowColour { get; set; } = new(0.22f, 0.36f, 0.32f);
    [Export] public Color DeepColour { get; set; } = new(0.075f, 0.19f, 0.22f);
    [Export] public Color HighlightColour { get; set; } = new(0.52f, 0.67f, 0.59f);
    [Export] public Color BankColour { get; set; } = new(0.30f, 0.28f, 0.22f);
    [Export(PropertyHint.Range, "0,2,0.05")] public float WaveSpeed { get; set; } = 0.65f;
    [ExportGroup("Wading response")]
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float MovementMultiplier { get; set; } = 0.70f;
    [Export(PropertyHint.Range, "0,24,1")] public float MaximumSubmergePixels { get; set; } = 14f;
    [Export(PropertyHint.Range, "10,150,5")] public float DepthDistance { get; set; } = 65f;
}
