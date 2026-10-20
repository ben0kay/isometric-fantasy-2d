// Disposable rolling-terrain test: 3D ground/collision, 2D tree cards, angled sunlight.
// All changes stay under DEBUG; the reusable player and camera systems remain unchanged.
using Godot;
using System.Collections.Generic;

public partial class NaturalTerrainTest : Node3D
{
    [ExportGroup("Test Settings")]
    [Export] public NaturalTerrainConfig Config { get; set; }
    [Export] public Texture2D TreeTexture { get; set; }

    private FastNoiseLite _noise;
    private readonly List<Vector2> _treePositions = new();
    private Node3D _terrainRoot;
    private Node3D _forestRoot;

    #region Scene setup
    // Build once: all ground cells become one mesh and one static concave collision shape.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new NaturalTerrainConfig();
        _noise = new FastNoiseLite { Seed = Config.TerrainSeed, Frequency = 0.045f, FractalType = FastNoiseLite.FractalTypeEnum.Fbm, FractalOctaves = 3 };
        _terrainRoot = GetNode<Node3D>("Terrain");
        _forestRoot = GetNode<Node3D>("Forest");
        BuildGround();
        BuildForest();
        ConfigureScene();
        GD.Print("[NaturalTerrainTest] Setup complete. Generated ground and " + _treePositions.Count + " trees. If you only see the low flat fallback floor, check the C# build and Godot Output panel.");
    }

    // Keep player speed changed for this test only (duplicate the shared resource).
    // =========================================================
    private void ConfigureScene()
    {
        PlayerMovement player = GetNode<PlayerMovement>("Player");
        player.Config = (PlayerMasterConfig)player.Config.Duplicate();
        player.Config.WalkSpeed = Config.PlayerWalkSpeed;
        player.Position = new Vector3(0, Elevation(0, 5) + 0.15f, 5);
        DirectionalLight3D sun = GetNode<DirectionalLight3D>("Lighting/Sun");
        sun.RotationDegrees = new Vector3(Config.SunPitch, Config.SunYaw, 0);
        sun.LightEnergy = Config.SunEnergy;
    }
    #endregion

    #region Ground generation
    // Smooth hills plus low-frequency noise; flatten only the initial spawn clearing.
    // =========================================================
    private float Elevation(float x, float z)
    {
        float hill = (Mathf.Sin(x * 0.075f) * Mathf.Cos(z * 0.055f) + 0.35f * Mathf.Sin((x + z) * 0.11f)) * Config.HillHeight;
        float noise = _noise.GetNoise2D(x, z) * Config.NoiseHeight;
        float flat = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((new Vector2(x, z) - new Vector2(0, 5)).Length() - 4f, 0f, 7f) / 7f);
        return (hill + noise) * flat;
    }

    // Shared mesh with vertex colors for patches; trimesh collision follows actual slopes.
    // =========================================================
    private void BuildGround()
    {
        int cols = Mathf.RoundToInt(Config.WorldWidth / Config.GridStep);
        int rows = Mathf.RoundToInt(Config.WorldDepth / Config.GridStep);
        float stepX = (float)Config.WorldWidth / cols;
        float stepZ = (float)Config.WorldDepth / rows;
        SurfaceTool surface = new();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        for (int z = 0; z < rows; z++)
        for (int x = 0; x < cols; x++)
        {
            float left = -Config.WorldWidth * 0.5f + x * stepX;
            float back = -Config.WorldDepth * 0.5f + z * stepZ;
            Vector3 a = new(left, Elevation(left, back), back);
            Vector3 b = new(left + stepX, Elevation(left + stepX, back), back);
            Vector3 c = new(left, Elevation(left, back + stepZ), back + stepZ);
            Vector3 d = new(left + stepX, Elevation(left + stepX, back + stepZ), back + stepZ);
            Color grass = GrassColor(left + stepX * 0.5f, back + stepZ * 0.5f);
            AddVertex(surface, a, grass); AddVertex(surface, c, grass); AddVertex(surface, b, grass);
            AddVertex(surface, b, grass); AddVertex(surface, c, grass); AddVertex(surface, d, grass);
        }
        surface.GenerateNormals();
        ArrayMesh mesh = surface.Commit();
        StandardMaterial3D material = new() { VertexColorUseAsAlbedo = true, Roughness = 0.95f };
        if (mesh == null || mesh.GetSurfaceCount() == 0) { GD.PushError("[NaturalTerrainTest] Terrain mesh creation returned no surfaces."); return; }
        mesh.SurfaceSetMaterial(0, material);
        StaticBody3D body = new() { Name = "GroundBody" };
        MeshInstance3D visuals = new() { Name = "TerrainMesh", Mesh = mesh };
        CollisionShape3D collision = new() { Name = "GroundCollision", Shape = mesh.CreateTrimeshShape() };
        _terrainRoot.AddChild(body);
        body.AddChild(visuals);
        body.AddChild(collision);
        GD.Print("[NaturalTerrainTest] Terrain surface and 3D collision created: " + cols + " x " + rows + " cells.");
    }

    // Emit a vertex with a consistent local color and tiled coordinates.
    // =========================================================
    private static void AddVertex(SurfaceTool surface, Vector3 point, Color color)
    {
        surface.SetColor(color);
        surface.SetUV(new Vector2(point.X * 0.13f, point.Z * 0.13f));
        surface.AddVertex(point);
    }

    // Gentle forest-green and earth patches; avoid one uniformly colored green plane.
    // =========================================================
    private Color GrassColor(float x, float z)
    {
        float patch = _noise.GetNoise2D(x + 200, z - 200);
        return new Color(0.27f + patch * 0.06f, 0.36f + patch * 0.075f, 0.245f + patch * 0.035f);
    }
    #endregion

    #region Sprite forest
    // Deterministic scatter with clearance around spawn and between trunks.
    // =========================================================
    private void BuildForest()
    {
        if (TreeTexture == null) { GD.PushWarning("NaturalTerrainTest: missing tree PNG texture."); return; }
        RandomNumberGenerator random = new() { Seed = (ulong)Config.TreeSeed };
        int attempts = Config.TreeCount * 30;
        for (int i = 0; i < attempts && _treePositions.Count < Config.TreeCount; i++)
        {
            float x = random.RandfRange(-Config.WorldWidth * 0.46f, Config.WorldWidth * 0.46f);
            float z = random.RandfRange(-Config.WorldDepth * 0.46f, Config.WorldDepth * 0.46f);
            Vector2 pos = new(x, z);
            if ((pos - new Vector2(0, 5)).Length() < 8f) continue;
            bool crowded = false;
            foreach (Vector2 existing in _treePositions)
                if (existing.DistanceSquaredTo(pos) < Config.MinimumTreeSpacing * Config.MinimumTreeSpacing) { crowded = true; break; }
            if (crowded) continue;
            _treePositions.Add(pos);
            MakeTree(x, z);
        }
    }

    // Upright camera-aligned card with a narrow cylindrical trunk collision.
    // =========================================================
    private void MakeTree(float x, float z)
    {
        StaticBody3D tree = new() { Name = "Tree_" + _treePositions.Count };
        tree.Position = new Vector3(x, Elevation(x, z), z);
        Sprite3D sprite = new()
        {
            Name = "TreeSprite", Texture = TreeTexture, Shaded = true, DoubleSided = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest, AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided
        };
        sprite.PixelSize = Config.TreeHeight / TreeTexture.GetHeight();
        sprite.Position = new Vector3(0, Config.TreeHeight * 0.5f, 0);
        sprite.RotationDegrees = new Vector3(0, 45, 0);
        CollisionShape3D collision = new()
        {
            Name = "TrunkCollision",
            Position = new Vector3(0, 0.85f, 0),
            Shape = new CylinderShape3D { Radius = 0.34f, Height = 1.7f }
        };
        _forestRoot.AddChild(tree);
        tree.AddChild(sprite);
        tree.AddChild(collision);
    }
    #endregion
}
