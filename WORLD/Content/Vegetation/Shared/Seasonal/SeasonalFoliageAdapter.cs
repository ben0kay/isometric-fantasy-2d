// World-owned seasonal palette textures and chunk-local material groups; no frame callbacks.
using Godot;
using System;
using System.Collections.Generic;
using Tree = FantasyGame.World.Trees.Tree;

public partial class SeasonalFoliageAdapter : Node
{
    public const int TimingGroups = 16;
    public SeasonController Seasons { get; set; }
    public WindController Wind { get; set; }
    private sealed class PaletteState
    {
        public readonly Image Image = Godot.Image.CreateEmpty(TimingGroups, 2, false, Godot.Image.Format.Rgbaf);
        public ImageTexture Texture;
        public int Users;
        public readonly Color[] Last = new Color[TimingGroups * 2];
        public bool Written;
    }
    private sealed class ChunkState
    {
        public readonly Dictionary<Sprite2D, (Material Source, ShaderMaterial Owned)> Sprites = new();
        public readonly List<ShaderMaterial> Materials = new();
        public readonly List<SeasonalFoliageProfile> Profiles = new();
        public readonly List<TreeVisualVariant> Variants = new();
    }
    private readonly Dictionary<SeasonalFoliageProfile, PaletteState> _palettes = new();
    private readonly Dictionary<Node2D, ChunkState> _chunks = new();
    private readonly Dictionary<TreeVisualVariant, (Texture2D Mask, int Users)> _masks = new();
    public int PaletteCount => _palettes.Count;
    public int ChunkCount => _chunks.Count;

    // Only explicit season changes update tiny shared data textures, including snow/fire copies.
    // =========================================================
    public override void _Ready()
    { Seasons.Changed += Refresh; SetProcess(false); SetPhysicsProcess(false); }

    // Bind after wind, before wetness/snow. Groups scale with chunk/source/mask/profile, not trees.
    // =========================================================
    public void Attach(Node2D actors)
    {
        if (_chunks.ContainsKey(actors)) return;
        var state = new ChunkState(); _chunks.Add(actors, state);
        var groups = new Dictionary<(ShaderMaterial, Texture2D, SeasonalFoliageProfile), ShaderMaterial>();
        foreach (Node node in WorldActorSources.Initial(actors))
        {
            if (node is not Tree tree || !tree.Definition.HasFoliage) continue;
            Sprite2D sprite = tree.GetNode<Sprite2D>("Sprite");
            if (sprite.Material is not ShaderMaterial source || source.Shader == null
                || !source.Shader.Code.Contains("SeasonalFoliage.gdshaderinc")) continue;
            TreeVisualVariant variant = tree.SelectedVariant;
            Texture2D mask = AcquireMask(variant); state.Variants.Add(variant);
            SeasonalFoliageProfile profile = tree.Definition.SeasonalFoliage;
            if (mask == null && profile == null) continue;
            string identity = tree.HasMeta("world_instance_id") ? tree.GetMeta("world_instance_id").AsString()
                : FormattableString.Invariant($"{tree.Definition.Id}/{tree.GlobalPosition.X:R}/{tree.GlobalPosition.Y:R}");
            sprite.SetInstanceShaderParameter("seasonal_bucket", (float)TimingBucket(identity));
            var key = (source, mask, profile);
            if (!groups.TryGetValue(key, out ShaderMaterial material))
            {
                material = (ShaderMaterial)source.Duplicate();
                material.SetShaderParameter("foliage_has_mask", mask != null);
                if (mask != null) material.SetShaderParameter("foliage_mask", mask);
                material.SetShaderParameter("seasonal_profile_enabled", profile != null);
                if (profile != null)
                {
                    material.SetShaderParameter("seasonal_state", AcquirePalette(profile));
                    state.Profiles.Add(profile);
                }
                groups.Add(key, material); state.Materials.Add(material); Wind.RegisterMaterial(material);
            }
            state.Sprites.Add(sprite, (source, material)); sprite.Material = material;
        }
    }

    // Validate once while a variant is loaded; dimensions are metadata, with no CPU image readback.
    // =========================================================
    private Texture2D AcquireMask(TreeVisualVariant variant)
    {
        if (_masks.TryGetValue(variant, out var cached))
        { _masks[variant] = (cached.Mask, cached.Users + 1); return cached.Mask; }
        Texture2D mask = variant.FoliageMask;
        if (mask != null && mask.GetSize() != variant.Texture.GetSize())
        { GD.PushWarning($"Foliage mask for variant {variant.Id} must match source dimensions; using colour detection."); mask = null; }
        _masks.Add(variant, (mask, 1)); return mask;
    }

    // Shared texture identity survives material duplication; slider changes never rebuild materials.
    // =========================================================
    private ImageTexture AcquirePalette(SeasonalFoliageProfile profile)
    {
        if (!_palettes.TryGetValue(profile, out PaletteState state))
        {
            state = new PaletteState(); _palettes.Add(profile, state);
            WritePalette(profile, state); state.Texture = ImageTexture.CreateFromImage(state.Image);
        }
        state.Users++; return state.Texture;
    }

    // No tree scans: only currently referenced species palettes receive changes.
    // =========================================================
    private void Refresh()
    { foreach (var pair in _palettes) WritePalette(pair.Key, pair.Value); }

    // Fill a fixed 16x2 atlas: row 0 target RGB/influence, row 1 saturation/brightness.
    // Every material copy shares this texture; uploads happen once per changed profile.
    // =========================================================
    private void WritePalette(SeasonalFoliageProfile profile, PaletteState state)
    {
        bool dirty = !state.Written;
        for (int bucket = 0; bucket < TimingGroups; bucket++)
        {
            float offset = (bucket * 2f / (TimingGroups - 1) - 1f) * Seasons.TimingVariationDays
                * Mathf.Clamp(profile.TimingVariationScale, 0f, 2f) / 365f;
            float year = Mathf.PosMod(Seasons.YearPosition + offset, 1f);
            Color colour = profile.Palette?.Sample(year) ?? new Color(1, 1, 1, 0);
            colour.A = Seasons.Enabled ? Mathf.Clamp(colour.A, 0f, 1f) * Seasons.ColourStrength : 0f;
            Color tone = profile.ToneControls?.Sample(year) ?? new Color(1, 0, 0, 0);
            tone = new Color(Mathf.Clamp(tone.R, 0f, 1f), Mathf.Clamp(tone.G, 0f, 0.5f), 0, 0);
            if (colour.A == 0f) { colour = new Color(1, 1, 1, 0); tone = new Color(1, 0, 0, 0); }
            if (!state.Written || state.Last[bucket] != colour || state.Last[bucket + TimingGroups] != tone)
            {
                state.Last[bucket] = colour; state.Last[bucket + TimingGroups] = tone;
                state.Image.SetPixel(bucket, 0, colour); state.Image.SetPixel(bucket, 1, tone); dirty = true;
            }
        }
        state.Written = true;
        if (dirty) state.Texture?.Update(state.Image);
    }

    // Explicit FNV-1a avoids randomized string hashes, runtime IDs and reload-dependent RNG.
    // =========================================================
    public static int TimingBucket(string identity)
    {
        uint hash = 2166136261;
        foreach (char character in identity) hash = unchecked((hash ^ character) * 16777619);
        return (int)(hash % TimingGroups);
    }

    // Release exact chunk registrations after weather overlays detach; no unbounded static cache.
    // =========================================================
    public void Detach(Node2D actors)
    {
        if (!_chunks.Remove(actors, out ChunkState state)) return;
        foreach (var pair in state.Sprites)
            if (GodotObject.IsInstanceValid(pair.Key) && pair.Key.Material == pair.Value.Owned)
                pair.Key.Material = pair.Value.Source;
        foreach (ShaderMaterial material in state.Materials) Wind.UnregisterMaterial(material);
        foreach (var profile in state.Profiles)
            if (--_palettes[profile].Users == 0) _palettes.Remove(profile);
        foreach (var variant in state.Variants)
        {
            var cached = _masks[variant];
            if (cached.Users == 1) _masks.Remove(variant);
            else _masks[variant] = (cached.Mask, cached.Users - 1);
        }
    }

    // Scene shutdown unsubscribes before the controller is freed, then drops all world references.
    // =========================================================
    public override void _ExitTree()
    {
        Seasons.Changed -= Refresh;
        foreach (var state in _chunks.Values)
            foreach (var material in state.Materials) Wind.UnregisterMaterial(material);
        _chunks.Clear(); _palettes.Clear(); _masks.Clear();
    }
}
