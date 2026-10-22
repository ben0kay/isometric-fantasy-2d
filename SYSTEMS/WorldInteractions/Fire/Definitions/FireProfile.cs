// Per-object burn rules; artwork is supplied by the object's appearance adapter.
using Godot;
namespace FantasyGame.WorldInteractions.Fire;
[GlobalClass]
public partial class FireProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float IgnitionDelaySeconds { get; set; } = 1f;
    [Export] public float TransitionSeconds { get; set; } = 5f;
    [Export] public bool MayReignite { get; set; }
}
