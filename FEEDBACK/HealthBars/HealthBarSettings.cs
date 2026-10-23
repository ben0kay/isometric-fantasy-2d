// Shared, inspector-tunable health bar presentation for all ActorHealth owners.
using Godot;

[GlobalClass]
public partial class HealthBarSettings : Resource
{
    [ExportGroup("Visibility")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool ShowAtFullHealth { get; set; } = false;
    [ExportGroup("Sizing")]
    [Export(PropertyHint.Range, "16,160,1")] public float MinimumWidth { get; set; } = 36f;
    [Export(PropertyHint.Range, "16,200,1")] public float MaximumWidth { get; set; } = 92f;
    [Export] public int HealthAtMinimumWidth { get; set; } = 50;
    [Export] public int HealthAtMaximumWidth { get; set; } = 2000;
    [Export(PropertyHint.Range, "2,12,1")] public float BarHeight { get; set; } = 5f;
    [Export(PropertyHint.Range, "0,60,1")] public float VerticalGap { get; set; } = 8f;
    [Export(PropertyHint.Range, "-80,80,1")] public float VerticalOffset { get; set; } = 0f;
    [ExportGroup("Colors")]
    [Export] public Color Background { get; set; } = new("#171b1d");
    [Export] public Color Border { get; set; } = new("#70796a");
    [Export] public Color Healthy { get; set; } = new("#66b66b");
    [Export] public Color Warning { get; set; } = new("#d8af55");
    [Export] public Color Critical { get; set; } = new("#d76b60");
    [Export(PropertyHint.Range, "0,1,0.01")] public float WarningThreshold { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float CriticalThreshold { get; set; } = 0.25f;
    [ExportGroup("Transition")]
    [Export] public bool SmoothChanges { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.01")] public float AnimationSeconds { get; set; } = 0.16f;
}
