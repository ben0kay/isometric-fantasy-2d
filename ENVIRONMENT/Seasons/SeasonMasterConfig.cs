// Authored defaults for the manual season preview; runtime commands never mutate this Resource.
using Godot;

[GlobalClass]
public partial class SeasonMasterConfig : Resource
{
    [ExportGroup("Seasonal foliage preview")]
    [Export] public bool Enabled { get; set; }
    [Export(PropertyHint.Range, "0,1,0.001")] public float YearPosition { get; set; } = 0.25f;
}
