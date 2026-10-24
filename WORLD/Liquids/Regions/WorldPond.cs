// Composition adapter for the existing DEBUG pond; production systems remain independently owned.
using Godot;
public partial class WorldPond : Node2D
{
    private WorldWeather _weather;
    public WorldLiquidRegion Region { get; private set; }
    public void Initialize(PlayerMovement player, WorldWeather weather, GeneratedWaterRuntime generated = null)
    {
        _weather = weather; Region = GetNode<WorldLiquidRegion>("Pond");
        AddChild(new WorldLiquidWading { Name = "Wading", Region = Region, Player = player, RegionAt = generated == null ? null : generated.RegionAt });
        Region.AddChild(new WorldLiquidRainSplashes {
            Name = "RainSplashes", Region = Region, Weather = weather, Config = Region.Config, ZIndex = -4
        });
    }
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldPond.Process");
        if (Region == null) return;
        Region.UpdateSurface(delta, _weather?.WindStrength ?? 0f);
    }
}
