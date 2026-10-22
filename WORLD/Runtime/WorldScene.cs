// Composes a bounded adjacent-biome world from reusable production modules; no debug dependencies.
using Godot;
using FantasyGame.WorldInteractions.Fire;

public partial class WorldScene : Node2D
{
    [Export] public WorldMasterConfig Config { get; set; }
    public int ActiveSeed { get; private set; }

    #region Setup
    // Choose a fresh seed once per load, then pass only that seed into deterministic generation.
    // =========================================================
    public override void _Ready()
    {
        if (Config?.Biome?.Surface?.Texture == null || Config.Biome.Surface.Material == null
            || (Config.Biomes.Secondary != null && Config.Biomes.Secondary.Surface == null)
            || Config.Bounds.Size.X <= 0f || Config.Bounds.Size.Y <= 0f
            || !Config.Bounds.HasPoint(Config.PlayerSpawn))
        {
            GD.PushError("World requires a biome/surface, positive bounds and a spawn inside them."); return;
        }
        var seedSource = new RandomNumberGenerator();
        if (Config.RandomSeedOnLoad) seedSource.Randomize();
        ActiveSeed = Config.RandomSeedOnLoad ? (int)(seedSource.Randi() & 0x7fffffff) : Config.FixedSeed;
        GD.Print($"World seed: {ActiveSeed} | {Config.Biome.Id} | random on load: {Config.RandomSeedOnLoad}");
        GetNode<GroundSurface>("Ground").Configure(Config.Bounds, Config.Biome.Surface, Config.Biomes);
        CharacterBody2D player = GetNode<CharacterBody2D>("Actors/Player");
        player.Position = Config.PlayerSpawn;
        Camera2D camera = player.GetNode<Camera2D>("Camera");
        camera.LimitLeft = Mathf.FloorToInt(Config.Bounds.Position.X);
        camera.LimitTop = Mathf.FloorToInt(Config.Bounds.Position.Y);
        camera.LimitRight = Mathf.CeilToInt(Config.Bounds.End.X);
        camera.LimitBottom = Mathf.CeilToInt(Config.Bounds.End.Y);
        AddBounds();
        var pond = GetNodeOrNull<WorldPond>("Liquids");
        var placement = GetNode<WorldSceneryPlacement>("Actors");
        placement.ExcludedLiquid = pond?.GetNodeOrNull<WorldLiquidRegion>("Pond");
        placement.Generate(Config, ActiveSeed);
        // Build entity navigation only after the seeded tree/rock colliders exist.
        GetNodeOrNull<WorldEntityPlacement>("EntityPlacement")?.Populate(placement, player, Config.Bounds);
        var environment = GetNodeOrNull<EnvironmentRuntime>("Environment");
        environment?.Initialize(Config, GetNode<Node2D>("Actors"));
        if (environment != null) GetNode<FireRuntime>("Fire").Initialize(GetNode<Node2D>("Actors"), environment);
        if (pond != null && environment?.Weather != null) pond.Initialize(GetNode<PlayerMovement>("Actors/Player"), environment.Weather);
    }

    // Static boundary walls derive from the same bounds as ground and camera.
    // =========================================================
    private void AddBounds()
    {
        Rect2 b = Config.Bounds;
        var walls = new StaticBody2D { Name = "Bounds" }; AddChild(walls);
        AddWall(walls, new(b.GetCenter().X, b.Position.Y - 16), new(b.Size.X + 64, 32));
        AddWall(walls, new(b.GetCenter().X, b.End.Y + 16), new(b.Size.X + 64, 32));
        AddWall(walls, new(b.Position.X - 16, b.GetCenter().Y), new(32, b.Size.Y + 64));
        AddWall(walls, new(b.End.X + 16, b.GetCenter().Y), new(32, b.Size.Y + 64));
    }

    // Group the four simple shapes under one body to keep world composition readable.
    // =========================================================
    private static void AddWall(StaticBody2D parent, Vector2 position, Vector2 size) =>
        parent.AddChild(new CollisionShape2D { Position = position, Shape = new RectangleShape2D { Size = size } });
    #endregion
}
