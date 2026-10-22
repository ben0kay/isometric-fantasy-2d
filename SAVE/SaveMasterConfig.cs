// Save policy belongs to SAVE; generation and scenery settings remain in WORLD.
using Godot;
[GlobalClass]
public partial class SaveMasterConfig : Resource
{
    [ExportGroup("Checkpoints")]
    [Export(PropertyHint.Range, "0,1800,10")] public float AutosaveSeconds { get; set; } = 120;
    [ExportGroup("Validation limits")]
    [Export] public int MaximumFileMegabytes { get; set; } = 32;
    [Export] public int MaximumChangedChunks { get; set; } = 32768;
}
