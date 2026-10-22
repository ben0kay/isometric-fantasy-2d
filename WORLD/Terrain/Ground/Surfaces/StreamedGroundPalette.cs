// One small macro-palette texture set serves every streamed ground quad, for any biome collection.
using Godot;
using System.Collections.Generic;
using System.Linq;
public static class StreamedGroundPalette
{
    // Upload coarse appearance once; no full-world geometry, per-frame uploads or per-biome shader branches.
    // =========================================================
    public static ShaderMaterial Build(MacroWorldMap map, IEnumerable<WorldBiomeDefinition> definitions)
    {
        var profiles = definitions.Where(b => b != null && b.EnabledForGeneration).ToDictionary(b => b.Id, b => b.Surface);
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/Terrain/Ground/Surfaces/Shaders/StreamedGround.gdshader") };
        int atlasColumns = Mathf.CeilToInt(Mathf.Sqrt(map.Biomes.Count));
        using (var atlas = Image.CreateEmpty(atlasColumns*32,atlasColumns*32,false,Image.Format.Rgba8))
        {
            for (int i = 0; i < map.Biomes.Count; i++)
            {
                var profile = profiles[map.Biomes[i].Id];
                if (profile?.Texture == null) throw new System.ArgumentException("Streamed biomes need surface textures.");
                using var tile = profile.Texture.GetImage(); tile.Convert(Image.Format.Rgba8); tile.Resize(32,32,Image.Interpolation.Nearest);
                atlas.BlitRect(tile,new Rect2I(0,0,32,32),new Vector2I(i%atlasColumns*32,i/atlasColumns*32));
            }
            material.SetShaderParameter("ground_art",ImageTexture.CreateFromImage(atlas));
        }
        material.SetShaderParameter("art_columns",(float)atlasColumns);
        material.SetShaderParameter("ground_noise",GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres"));
        string[] names = { "map_base","map_shade","map_fresh","map_dry","map_settings" };
        for (int channel = 0; channel < names.Length; channel++)
        {
            using var image = Image.CreateEmpty(map.Columns,map.Rows,false,Image.Format.Rgbaf);
            for (int y = 0; y < map.Rows; y++) for (int x = 0; x < map.Columns; x++)
            {
                var p = profiles[map.Biomes[map.Regions[map.RegionIdAtCell(x,y)].BiomeIndex].Id];
                if (p == null) throw new System.ArgumentException("Every streamed biome requires a surface profile.");
                Color value = channel switch { 0 => p.BaseColour, 1 => p.ShadeColour, 2 => p.FreshColour, 3 => p.DryColour, _ => new Color(p.Variation,p.PatchSize,p.DetailStrength,map.Regions[map.RegionIdAtCell(x,y)].BiomeIndex) };
                image.SetPixel(x,y,value);
            }
            material.SetShaderParameter(names[channel],ImageTexture.CreateFromImage(image));
        }
        material.SetShaderParameter("map_origin",map.Bounds.Position); material.SetShaderParameter("map_cell_size",map.CellSize);
        material.SetShaderParameter("map_grid_size",new Vector2(map.Columns,map.Rows)); return material;
    }
    // Each exact clipped chunk rectangle shares the palette and world-space texture coordinates.
    // =========================================================
    public static MeshInstance2D Create(Rect2 bounds, ShaderMaterial material) => new() { Name = "Ground", Mesh = new QuadMesh { Size = bounds.Size }, Position = bounds.GetCenter(), Material = material, ZIndex = -10 };
}
