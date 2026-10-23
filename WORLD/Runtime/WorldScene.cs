// Composes a finite streamed world or the compatibility preview from reusable production modules.
using Godot;
using System;
using System.Linq;
using FantasyGame.WorldInteractions.Fire;

public partial class WorldScene : Node2D
{
    [Export] public WorldMasterConfig Config { get; set; }
    public WorldFeaturePlan FeaturePlan { get; private set; }
    public MacroWorldMap MacroMap { get; private set; }
    public WorldBlueprintManifest Blueprint { get; private set; }
    public WorldChunkStream Stream { get; private set; }
    public int ActiveSeed { get; private set; }

    public CampaignSaveController Saves { get; private set; }
    public string StartupError { get; private set; }

    // Explicit ownership destinations are cached once, not rediscovered during frame callbacks.
    public Node2D ActorsRoot { get; private set; }
    public Node2D EntitiesRoot { get; private set; }
    public Node2D TestActorsRoot { get; private set; }
    public Node2D PropsRoot { get; private set; }
    public Node2D ChunkActorsRoot { get; private set; }
    public Node2D PreviewSceneryRoot { get; private set; }
    public Node2D StreamedGroundRoot { get; private set; }
    public PlayerMovement Player { get; private set; }
    public WorldSceneryPlacement SceneryPlacement { get; private set; }
    public WorldEntityPlacement EntityPlacement { get; private set; }
    public EnvironmentRuntime Environment { get; private set; }
    public FireRuntime Fire { get; private set; }
    private ProjectileSpawner _projectiles;
    // Lazily share one world budget; shots are not scenery-chunk children and never request generation.
    // =========================================================
    public ProjectileSpawner GetProjectileSpawner()
    {
        if(GodotObject.IsInstanceValid(_projectiles)) return _projectiles;
        _projectiles=new ProjectileSpawner { Name="Projectiles",Bounds=MacroMap?.Bounds ?? Config.Bounds };
        _projectiles.IsPositionLoaded=point=>Stream==null || Stream.IsPositionLoaded(point);
        _projectiles.IsSegmentLoaded=(from,to)=>Stream==null || Stream.IsSegmentLoaded(from,to);
        _projectiles.SetMeta("world_actor_container",true);
        ActorsRoot.AddChild(_projectiles); return _projectiles;
    }

    // PackedScene children exist before tree entry; cache before child debug panels run Ready.
    // =========================================================
    public override void _EnterTree()
    {
        ActorsRoot = GetNode<Node2D>("WorldActorsRoot");
        EntitiesRoot = ActorsRoot.GetNode<Node2D>("PersistentEntitiesRoot");
        TestActorsRoot = EntitiesRoot.GetNodeOrNull<Node2D>("DebugActorsRoot") ?? EntitiesRoot;
        PropsRoot = ActorsRoot.GetNode<Node2D>("PersistentPropsRoot");
        ChunkActorsRoot = ActorsRoot.GetNode<Node2D>("ChunkActorsRoot");
        PreviewSceneryRoot = ActorsRoot.GetNode<Node2D>("PreviewSceneryRoot");
        StreamedGroundRoot = GetNode<Node2D>("TerrainRoot/StreamedGroundRoot");
        Player = EntitiesRoot.GetNode<PlayerMovement>("Player");
        SceneryPlacement = GetNode<WorldSceneryPlacement>("SystemsRoot/SceneryPlacement");
        EntityPlacement = GetNode<WorldEntityPlacement>("SystemsRoot/EntityPlacement");
        Environment = GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        Fire = GetNode<FireRuntime>("SystemsRoot/Fire");
    }

    #region Setup
    // Choose a fresh seed once per load, then pass only that seed into deterministic generation.
    // =========================================================
    public override void _Ready()
    {
        var launch = CampaignSession.Consume();
        try { InitializeWorld(launch); }
        catch (Exception error)
        {
            StartupError = error.Message; GD.PushError(error.ToString());
            Player.ProcessMode = ProcessModeEnum.Disabled;
            if (launch != null)
            {
                var failure = new AcceptDialog { DialogText = "Campaign could not start: " + error.Message };
                failure.Confirmed += () => MenuNavigation.Open(this,MenuNavigation.Main);
                failure.Canceled += () => MenuNavigation.Open(this,MenuNavigation.Main);
                AddChild(failure); failure.PopupCentered();
            }
        }
    }
    // Freeze identity before streaming; saved macro outcomes are restored, never regenerated.
    // =========================================================
    private void InitializeWorld(CampaignLaunch launch)
    {
        foreach (var root in new[] { ActorsRoot,EntitiesRoot,PropsRoot,ChunkActorsRoot,PreviewSceneryRoot,StreamedGroundRoot })
            if (!root.Transform.IsEqualApprox(Transform2D.Identity)) throw new InvalidOperationException("World ownership containers must retain identity transforms: " + root.Name);
        bool wantsStreaming = Config?.Streaming?.Enabled == true;
        if (Config == null || (wantsStreaming && Config.Generation == null)
            || (!wantsStreaming && (Config.Biome?.Surface?.Texture == null || Config.Biome.Surface.Material == null
                || Config.Bounds.Size.X <= 0 || Config.Bounds.Size.Y <= 0 || !Config.Bounds.HasPoint(Config.PlayerSpawn))))
        {
            GD.PushError("World requires a generation config for streaming, or valid preview biome/bounds/spawn."); return;
        }
        var restore = launch?.Restore;
        if (launch != null && !wantsStreaming) throw new InvalidOperationException("Campaign saves require streamed production mode.");
        if (restore != null) { CampaignSession.Store().Validate(restore); CampaignSession.RequireContent(restore); }
        var seedSource = new RandomNumberGenerator();
        if (Config.RandomSeedOnLoad) seedSource.Randomize();
        ActiveSeed = launch?.Seed ?? (Config.RandomSeedOnLoad ? (int)(seedSource.Randi() & 0x7fffffff) : Config.FixedSeed);
        GD.Print($"World seed: {ActiveSeed} | {Config.Biome?.Id ?? "streamed"} | random on load: {Config.RandomSeedOnLoad}");
        if (Config.Generation != null)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var features = restore == null ? WorldFeaturePlanner.Build(Config.Generation, ActiveSeed) : WorldBlueprintRestore.Features(restore.Blueprint);
                var map = restore == null ? MacroWorldBuilder.Build(Config.Generation, ActiveSeed) : WorldBlueprintRestore.Map(restore.Blueprint);
                FeaturePlan = features; MacroMap = map;
                Blueprint = restore?.Blueprint ?? (launch == null ? WorldBlueprintManifest.CreatePreview(MacroMap, FeaturePlan)
                    : WorldBlueprintManifest.Create(launch.ProfileId,launch.RunId,MacroMap,FeaturePlan) with { WorldId = launch.WorldId });
                foreach (string warning in features.Warnings) GD.PushWarning(warning);
                GD.Print($"World features | {features.Reservations.Features.Count} placed | hash {features.Hash}");
                GD.Print($"Macro map | {MacroMap.Columns}x{MacroMap.Rows} | {MacroMap.Regions.Count} regions | 100% land | {timer.ElapsedMilliseconds}ms | hash {MacroMap.Hash}");
            }
            catch (System.ArgumentException error) { throw new InvalidOperationException("Macro generation failed: " + error.Message,error); }
        }
        if (wantsStreaming && MacroMap == null)
        { Player.ProcessMode = ProcessModeEnum.Disabled; return; }
        bool streaming = wantsStreaming;
        if (streaming)
        {
            Config = (WorldMasterConfig)Config.Duplicate();
            Config.Bounds = MacroMap.Bounds; Config.PlayerSpawn = FeaturePlan.StartingArea?.Anchor ?? Config.Bounds.GetCenter();
            Config.Biomes = Config.Biomes == null ? new AdjacentBiomeLayout() : (AdjacentBiomeLayout)Config.Biomes.Duplicate();
            string biomeId = MacroMap.BiomeAt(restore == null ? Config.PlayerSpawn : new Vector2(restore.Player.X,restore.Player.Y)).Id;
            foreach (var biome in Config.Generation.Biomes) if (biome.Id == biomeId) Config.Biomes.Primary = biome;
            GetNode<GroundSurface>("TerrainRoot/Ground").Visible = false;
        }
        else GetNode<GroundSurface>("TerrainRoot/Ground").Configure(Config.Bounds, Config.Biome.Surface, Config.Biomes);
        CharacterBody2D player = Player;
        player.Position = restore == null ? Config.PlayerSpawn : new Vector2(restore.Player.X,restore.Player.Y);
        Camera2D camera = player.GetNode<Camera2D>("Camera");
        camera.LimitLeft = Mathf.FloorToInt(Config.Bounds.Position.X);
        camera.LimitTop = Mathf.FloorToInt(Config.Bounds.Position.Y);
        camera.LimitRight = Mathf.CeilToInt(Config.Bounds.End.X);
        camera.LimitBottom = Mathf.CeilToInt(Config.Bounds.End.Y);
        AddBounds();
        var pond = GetNodeOrNull<WorldPond>("TerrainRoot/Liquids");
        var placement = SceneryPlacement;
        placement.ExcludedLiquid = pond?.GetNodeOrNull<WorldLiquidRegion>("Pond");
        if (streaming)
        {
            Stream = new WorldChunkStream { Name = "Streaming" }; GetNode("SystemsRoot").AddChild(Stream);
            Stream.Initialize(this,ChunkActorsRoot,StreamedGroundRoot,ActorsRoot,EntityPlacement,(PlayerMovement)player,Config,placement.ExcludedLiquid,restore?.Chunks);
            Blueprint = Blueprint with { SchemaVersion = 4,
                MacroRegions = MacroMap.Regions.Select(r => new SavedMacroRegion(r.Id,r.BiomeIndex,r.Centre.X,r.Centre.Y,r.TargetDiameter,r.CellCount)).ToArray(),
                MacroBiomes = MacroMap.Biomes.Select(b => new SavedMacroBiome(b.Id,b.Name,b.Colour.R,b.Colour.G,b.Colour.B,b.Colour.A)).ToArray(), LocalGeneratorVersion = ChunkSceneryData.GeneratorVersion,
                ChunkSize = Config.Streaming.ChunkSize, CandidateCellSize = Config.Streaming.CandidateCellSize };
        }
        else placement.Generate(Config, ActiveSeed,PreviewSceneryRoot);
        // Build entity navigation only after the seeded tree/rock colliders exist.
        EntityPlacement.Populate(TestActorsRoot,ActorsRoot,player, Stream?.LocalBounds ?? Config.Bounds,restore?.TestEntity,includePractice: launch==null,projectiles: EntityPlacement.EntityDefinition?.Ranged!=null ? GetProjectileSpawner() : null);
        var environment = Environment;
        environment.Initialize(Config,ActorsRoot,PropsRoot,Player);
        if (environment != null) Fire.Initialize(ActorsRoot,environment);
        if (pond != null && environment?.Weather != null) pond.Initialize(Player, environment.Weather);
        var visibility = new OcclusionController { Name = "Occlusion" };
        GetNode("SystemsRoot").AddChild(visibility);
        visibility.Initialize(player);
        Stream?.Bind(environment,Fire,visibility);
        if (launch != null)
        {
            if (restore != null)
            {
                Player.HealthState.Restore(restore.Player.Health ?? Player.HealthState.Maximum);
                environment.Clock.Config.CycleSeconds = restore.Clock.CycleSeconds;
                environment.Clock.Config.Paused = restore.Clock.Paused; environment.Clock.SetHour(restore.Clock.Hour);
            }
            Saves = new CampaignSaveController { Name = "CampaignSave" }; GetNode("SystemsRoot").AddChild(Saves);
            Saves.Initialize(this,restore == null,CampaignSession.ContentFingerprint());
        }
    }

    // Static boundary walls derive from the same bounds as ground and camera.
    // =========================================================
    private void AddBounds()
    {
        Rect2 b = Config.Bounds;
        var walls = new StaticBody2D { Name = "Bounds", CollisionLayer = 1, CollisionMask = 1 }; GetNode("TerrainRoot").AddChild(walls);
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
