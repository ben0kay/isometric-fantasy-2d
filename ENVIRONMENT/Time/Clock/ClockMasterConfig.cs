// One editable accelerated clock shared by environmental renderers.
using Godot;
[GlobalClass]
public partial class ClockMasterConfig : Resource
{
    [Export] public float StartHour { get; set; } = 8f;
    [Export(PropertyHint.Range, "5,600,1")] public float CycleSeconds { get; set; } = 30f;
    [Export] public bool Paused { get; set; }
}
