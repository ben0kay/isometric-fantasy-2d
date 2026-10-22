// Composition only: wires independently owned environment modules after world scenery generation.
using Godot;

public partial class EnvironmentRuntime : Node2D
{
    [Export] public EnvironmentMasterConfig Config { get; set; }
    public BiomeEnvironmentProfile Settings { get; private set; }
    public GameClock Clock { get; private set; }
    public WindController Wind { get; private set; }
    public WorldWeather Weather { get; private set; }
    public WorldDayNight DayNight { get; private set; }
    public WorldSnow Snow { get; private set; }
    public bool Initialized { get; private set; }

    #region Composition
    // Called once by the production world after ground, spawn and scenery are configured.
    // =========================================================
    public void Initialize(WorldMasterConfig world, Node2D actors)
    {
        if (Initialized) return;
        Config ??= GD.Load<EnvironmentMasterConfig>("res://ENVIRONMENT/DefaultEnvironmentConfig.tres");
        Settings = (world.Biome.Environment ?? new BiomeEnvironmentProfile()).Resolve(Config.Defaults);
        Clock = new GameClock { Name = "Clock", Config = (ClockMasterConfig)Config.Clock.Duplicate(true) }; AddChild(Clock);
        Wind = new WindController { Name = "Wind", Config = Settings.Wind }; AddChild(Wind);
        var vegetation = new VegetationWind { Name = "VegetationWind", Wind = Wind }; AddChild(vegetation); vegetation.Attach(actors);
        var torch = GD.Load<PackedScene>("res://WORLD/Content/Props/Torch/Torch.tscn").Instantiate<Node2D>();
        torch.Name = "Torch"; torch.Position = world.PlayerSpawn + Settings.Torch.OffsetFromSpawn; actors.AddChild(torch);
        AddChild(new TorchLight { Name = "TorchLight", Torch = torch, Config = Settings.Torch });
        // Temporary comparison lamps share the existing profile/controller; no new lighting manager.
        int previewIndex = 0;
        foreach (Vector2 offset in Settings.Torch.AdditionalOffsets)
        {
            if (++previewIndex > 2) break;
            Vector2 position = world.PlayerSpawn + offset;
            if (!world.Bounds.Grow(-20f).HasPoint(position)) continue;
            var extra = GD.Load<PackedScene>("res://WORLD/Content/Props/Torch/Torch.tscn").Instantiate<Node2D>();
            extra.Name = "PreviewTorch" + previewIndex; extra.Position = position; actors.AddChild(extra);
            AddChild(new TorchLight { Name = "PreviewTorchLight" + previewIndex, Torch = extra, Config = Settings.Torch });
        }
        Weather = new WorldWeather { Name = "Weather", Config = Settings.Weather, Wind = Wind, Actors = actors, WorldBounds = world.Bounds }; AddChild(Weather);
        // Wind is already attached; register leaf wetness before snow clones tree materials.
        var wetness = new VegetationWetness { Name = "VegetationWetness", Weather = Weather }; AddChild(wetness); wetness.Attach(actors);
        var shadows = new SunShadowRenderer { Name = "SunShadows", ZIndex = -5, Wind = Wind }; AddChild(shadows); shadows.Attach(actors);
        DayNight = new WorldDayNight { Name = "DayNight", Config = Settings.Lighting, Clock = Clock, Weather = Weather, Shadows = shadows }; AddChild(DayNight);
        var clouds = new CloudShade { Name = "Clouds", Config = Settings.Clouds, Day = DayNight, Wind = Wind }; clouds.Configure(world.Bounds); AddChild(clouds);
        var grade = GD.Load<PackedScene>("res://ENVIRONMENT/Lighting/Colour/ColourGrade.tscn").Instantiate<WorldColourGrade>();
        grade.Name = "ColourGrade"; grade.Config = Settings.ColourGrade; AddChild(grade);
        var atmosphere = GD.Load<PackedScene>("res://ENVIRONMENT/Atmosphere/Atmosphere.tscn").Instantiate<WorldAtmosphere>();
        atmosphere.Config = Settings.Atmosphere; atmosphere.Day = DayNight; atmosphere.Wind = Wind;
        atmosphere.Actors = actors; atmosphere.Torch = torch; atmosphere.WorldBounds = world.Bounds; AddChild(atmosphere);
        Snow = new WorldSnow { Name = "Snow", Config = Settings.Snow, Weather = Weather, Actors = actors, WorldBounds = world.Bounds }; AddChild(Snow);
        Initialized = true;
    }
    #endregion
}
