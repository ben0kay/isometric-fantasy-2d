// Composition adapter for the existing DEBUG pond; production systems remain independently owned.
using Godot;
public partial class WorldPond : Node2D
{
    public WorldLiquidRegion Region { get; private set; }
    public void Initialize(PlayerMovement player, WorldWeather weather)
    {
        Region = GetNode<WorldLiquidRegion>("Pond");
        AddChild(new WorldLiquidWading { Name = "Wading", Region = Region, Player = player });
        Region.AddChild(new WorldLiquidRainSplashes {
            Name = "RainSplashes", Region = Region, Weather = weather, Config = Region.Config, ZIndex = -4
        });
    }
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldPond.Process");
        if (Region == null) return;
        WorldWeather weather = GetNodeOrNull<WorldWeather>("../Environment/Weather");
        Region.UpdateSurface(delta, weather?.WindStrength ?? 0f);
    }
}
