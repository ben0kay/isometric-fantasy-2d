// One authorable collection of major-feature requests; referenced by the world generation master.
using Godot;

[GlobalClass]
public partial class WorldLayoutPreset : Resource
{
    [Export] public Godot.Collections.Array<WorldFeatureDirective> Features { get; set; } = new();
    [Export(PropertyHint.Range, "1,256,1")] public int AttemptsPerFeature { get; set; } = 64;
    [Export(PropertyHint.Range, "1,256,1")] public int MaximumFeatures { get; set; } = 128;
}
