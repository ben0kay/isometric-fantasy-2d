// Disposable snow appearance and snowfall controls; no production season/calendar dependency.
using Godot;

[GlobalClass]
public partial class SnowMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = false;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Coverage { get; set; } = 0.6f;
    [Export] public bool Snowfall { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.01")] public float SnowfallRate { get; set; } = 0.65f;
    [Export] public bool AutoAccumulate { get; set; } = false;
    [Export(PropertyHint.Range, "5,120,1")] public float AccumulationSeconds { get; set; } = 30f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float TransitionSeconds { get; set; } = 3f;
    [Export] public Color SnowColour { get; set; } = new(0.87f, 0.91f, 0.94f);
}
