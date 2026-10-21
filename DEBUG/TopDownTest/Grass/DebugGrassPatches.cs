// Seeds four grass-art variants in loose clumps at startup, with no collision.
// All sprites reuse one GPU TreeSway material connected to the existing Weather wind.
using Godot;
using System.Collections.Generic;

public partial class DebugGrassPatches : Node2D
{
    [ExportGroup("References")]
    [Export] public GrassPatchMasterConfig Config { get; set; }
    [Export] public ShaderMaterial WindMaterial { get; set; }
    [Export] public Godot.Collections.Array<Texture2D> GrassTextures { get; set; } = new();

    private readonly List<Vector2> _positions = new();
    private DebugWorldScatter _scatter;
    private DebugWeather _weather;
    private ShaderMaterial _wind;
    private float _lastWind = float.NaN;
    private int _placed;

    #region Setup
    // Actors have finished generating trees and rocks when this sibling's Ready runs.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new GrassPatchMasterConfig();
        _scatter = GetNode<DebugWorldScatter>("../Actors");
        _weather = GetNode<DebugWeather>("../Weather");
        if (_scatter.Config == null || WindMaterial == null || GrassTextures.Count == 0)
        {
            GD.PushWarning("Debug grass needs a config, wind material and grass sprites.");
            return;
        }
        _wind = (ShaderMaterial)WindMaterial.Duplicate();
        Rect2 bounds = _scatter.Config.WorldBounds;
        var rng = new RandomNumberGenerator { Seed = (ulong)(uint)(_scatter.Config.Seed ^ 0x376A91) };
        for (int c = 0; c < Mathf.Clamp(Config.ClusterCount, 0, 100); c++)
        {
            Vector2 center = RandomPoint(rng, bounds);
            int low = Mathf.Clamp(Mathf.Min(Config.ClumpsPerClusterMin, Config.ClumpsPerClusterMax), 1, 15);
            int high = Mathf.Clamp(Mathf.Max(Config.ClumpsPerClusterMin, Config.ClumpsPerClusterMax), low, 15);
            int count = rng.RandiRange(low, high);
            for (int i = 0; i < count; i++) TryPlace(rng, center, true, bounds);
        }
        for (int i = 0; i < Mathf.Clamp(Config.IsolatedClumps, 0, 200); i++)
            TryPlace(rng, Vector2.Zero, false, bounds);
        UpdateWind();
        GD.Print($"DEBUG grass | seed {_scatter.Config.Seed} | {_placed} tufts / {GrassTextures.Count} variants");
    }
    #endregion

    #region Placement
    // A small bounded retry loop avoids trees, rocks, paths and the clear starting area.
    // =========================================================
    private void TryPlace(RandomNumberGenerator rng, Vector2 center, bool clustered, Rect2 bounds)
    {
        for (int attempt = 0; attempt < Mathf.Clamp(Config.AttemptsPerClump, 1, 20); attempt++)
        {
            Vector2 p = clustered ? ClusterPoint(rng, center) : RandomPoint(rng, bounds);
            if (!bounds.Grow(-Mathf.Max(0f, Config.EdgeMargin)).HasPoint(p)) continue;
            if (!_scatter.CanPlaceGrassDecoration(p, Mathf.Max(0f, Config.ObstacleClearance))) continue;
            if (!HasRoom(p)) continue;
            AddTuft(rng, p);
            return;
        }
    }

    // Circular distribution looks more organic than a square grid of equal spacing.
    // =========================================================
    private Vector2 ClusterPoint(RandomNumberGenerator rng, Vector2 center)
    {
        float angle = rng.RandfRange(0f, Mathf.Tau);
        float radius = Mathf.Pow(rng.Randf(), 0.75f) * Mathf.Max(1f, Config.ClusterRadius);
        return center + Vector2.FromAngle(angle) * radius;
    }

    // Scattered single tufts use uniform coordinates over the existing world bounds.
    // =========================================================
    private static Vector2 RandomPoint(RandomNumberGenerator rng, Rect2 bounds) =>
        new(rng.RandfRange(bounds.Position.X, bounds.End.X),
            rng.RandfRange(bounds.Position.Y, bounds.End.Y));

    // A little overlapping grass is fine; reject exact clump piles.
    // =========================================================
    private bool HasRoom(Vector2 p)
    {
        float min = Mathf.Max(0f, Config.MinimumSpacing);
        foreach (Vector2 other in _positions)
            if (p.DistanceSquaredTo(other) < min * min) return false;
        return true;
    }

    // A single Sprite2D per clump; source PNGs are 128x128 with transparent margins.
    // =========================================================
    private void AddTuft(RandomNumberGenerator rng, Vector2 p)
    {
        Texture2D texture = GrassTextures[rng.RandiRange(0, GrassTextures.Count - 1)];
        if (texture == null) return;
        float scale = rng.RandfRange(Mathf.Min(Config.MinimumScale, Config.MaximumScale),
                                    Mathf.Max(Config.MinimumScale, Config.MaximumScale));
        float tint = rng.RandfRange(Mathf.Min(Config.BrightnessMin, Config.BrightnessMax),
                                   Mathf.Max(Config.BrightnessMin, Config.BrightnessMax));
        bool flipped = rng.Randf() < Config.MirrorChance;
        var sprite = new Sprite2D
        {
            Name = "Grass" + _placed, Texture = texture, Material = _wind,
            Position = p, Offset = new Vector2(-texture.GetWidth() / 2f, -116f),
            Centered = false, Scale = Vector2.One * scale, FlipH = flipped,
            Modulate = new Color(tint, tint, tint, 1f),
            TextureFilter = CanvasItem.TextureFilterEnum.Linear
        };
        AddChild(sprite);
        sprite.SetInstanceShaderParameter("wind_orientation", flipped ? -1f : 1f);
        _positions.Add(p);
        _placed++;
    }
    #endregion

    #region Wind
    // Only the existing Weather slider changes this shared shader parameter.
    // =========================================================
    public override void _Process(double delta) => UpdateWind();

    private void UpdateWind()
    {
        if (_wind == null) return;
        float wind = _weather.WindStrength;
        if (Mathf.IsEqualApprox(wind, _lastWind)) return;
        _wind.SetShaderParameter("wind_multiplier", wind);
        _lastWind = wind;
    }
    #endregion
}
