// Composition adapter for the existing DEBUG pond; production systems remain independently owned.
using Godot;
public partial class WorldPond : Node2D
{
    public LiquidRegion Region { get; private set; }
    public void Initialize(PlayerMovement player, WorldWeather weather)
    {
        Region = GetNode<LiquidRegion>("Pond");
        AddChild(new LiquidWading { Name = "Wading", Region = Region, Player = player });
        Region.AddChild(new LiquidRainSplashes {
            Name = "RainSplashes", Region = Region, Weather = weather, Config = Region.Config, ZIndex = -4
        });
    }
    // =========================================================
    public override void _Process(double delta)
    {
        if (Region == null) return;
        WorldWeather weather = GetNodeOrNull<WorldWeather>("../Environment/Weather");
        Region.UpdateSurface(delta, weather?.WindStrength ?? 0f);
    }
}