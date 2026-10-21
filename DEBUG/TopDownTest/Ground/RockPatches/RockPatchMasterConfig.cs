// Disposable gravel-ground settings, separate from grass, rock artwork and lighting.
using Godot;

[GlobalClass]
public partial class RockPatchMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.5,2,0.05")] public float Spread { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Opacity { get; set; } = 0.85f;
    [Export] public Color DarkGravel { get; set; } = new(0.24f, 0.255f, 0.23f);
    [Export] public Color LightGravel { get; set; } = new(0.43f, 0.435f, 0.39f);
}
