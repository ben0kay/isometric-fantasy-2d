// Inspector-tunable, shared world-space damage text animation settings.
using Godot;

[GlobalClass]
public partial class DamageNumberSettings : Resource
{
    [ExportGroup("Visibility")]
    [Export] public bool Enabled { get; set; } = true;
    [ExportGroup("Text")]
    [Export(PropertyHint.Range, "8,40,1")] public int FontSize { get; set; } = 15;
    [Export(PropertyHint.Range, "0,8,1")] public int OutlineSize { get; set; } = 2;
    [Export] public Color TextColor { get; set; } = new("#f0d4a0");
    [Export] public Color OutlineColor { get; set; } = new("#302422");
    [ExportGroup("Placement")]
    [Export(PropertyHint.Range, "0,80,1")] public float VerticalGap { get; set; } = 16f;
    [Export(PropertyHint.Range, "-100,100,1")] public float VerticalOffset { get; set; } = 0f;
    [ExportGroup("Animation")]
    [Export(PropertyHint.Range, "0.1,2,0.01")] public float DurationSeconds { get; set; } = 0.7f;
    [Export(PropertyHint.Range, "0,80,1")] public float RiseDistance { get; set; } = 24f;
    [Export(PropertyHint.Range, "0,0.6,0.01")] public float MergeWindowSeconds { get; set; } = 0.3f;
    [Export(PropertyHint.Range, "1,2,0.05")] public float PopScale { get; set; } = 1.2f;
}
