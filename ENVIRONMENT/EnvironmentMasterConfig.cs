// World-wide clock and fallback visual profiles; biome settings can override individual components.
using Godot;
[GlobalClass]
public partial class EnvironmentMasterConfig : Resource
{
    [Export] public ClockMasterConfig Clock { get; set; }
    [Export] public BiomeEnvironmentProfile Defaults { get; set; }
}
