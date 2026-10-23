// Shared material presentation: particle mechanics stay in ParticleProfile, material appearance/count here.
using Godot;
[GlobalClass]
public partial class HitFeedbackProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public ParticleProfile Particles { get; set; }
    [Export] public Color Colour { get; set; } = Colors.White;
    [Export(PropertyHint.Range,"0,12,1")] public int Count { get; set; } = 8;
    [Export] public Vector2 VisualOffset { get; set; } = new(0,-14);
}
