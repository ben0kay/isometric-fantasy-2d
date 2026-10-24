// One small macro-palette texture set serves every streamed ground quad, for any biome collection.
using Godot;
using System.Collections.Generic;
using System.Linq;
using System;
public static class StreamedGroundPalette
{
    // Upload coarse appearance once; no full-world geometry, per-frame uploads or per-biome shader branches.
    // =========================================================
    public static ShaderMaterial Build(MacroWorldMap map, IEnumerable<WorldBiomeDefinition> definitions)
    {
        var source=definitions.Where(b=>b!=null && b.EnabledForGeneration).ToDictionary(b=>b.Id);
        if(map.Biomes.Count<1 || map.Biomes.Count>256) throw new ArgumentException("Streamed ground supports 1-256 biome appearances.");
        var compiled=new CompiledGroundBiome[map.Biomes.Count];
        for(int i=0;i<compiled.Length;i++) compiled[i]=new CompiledGroundBiome(source[map.Biomes[i].Id]);
        bool hasTint = compiled.Any(b=>b.Tint!=Colors.White);
        bool hasStyles=compiled.Any(b=>b.Base.DetailStyle!=GroundDetailStyle.Organic || b.Base.ArtworkInfluence!=0.22f);
        bool blendArt=compiled.Skip(1).Any(b=>b.Base.Texture!=compiled[0].Base.Texture);
        bool hasOverlays=compiled.Any(b=>b.Overlays.Any(GroundOverlayPalette.CanCompile));
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/Terrain/Ground/Surfaces/Shaders/StreamedGround.gdshader") };
        var artwork=compiled.Select(b=>b.Base.Texture).ToList();
        var overlayArt=new int[compiled.Length,2];
        for(int i=0;i<compiled.Length;i++) for(int slot=0;slot<compiled[i].Overlays.Length;slot++)
            if(GroundOverlayPalette.CanCompile(compiled[i].Overlays[slot]))
            { overlayArt[i,slot]=artwork.Count; artwork.Add(compiled[i].Overlays[slot].Surface.Texture); }
        int atlasColumns = Mathf.CeilToInt(Mathf.Sqrt(artwork.Count));
        using (var atlas = Image.CreateEmpty(atlasColumns*32,atlasColumns*32,false,Image.Format.Rgba8))
        {
            for (int i = 0; i < artwork.Count; i++)
            {
                using var tile = artwork[i].GetImage(); tile.Convert(Image.Format.Rgba8); tile.Resize(32,32,Image.Interpolation.Nearest);
                atlas.BlitRect(tile,new Rect2I(0,0,32,32),new Vector2I(i%atlasColumns*32,i/atlasColumns*32));
            }
            material.SetShaderParameter("ground_art",ImageTexture.CreateFromImage(atlas));
        }
        material.SetShaderParameter("art_columns",(float)atlasColumns);
        material.SetShaderParameter("ground_noise",GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres"));
        material.SetShaderParameter("has_ground_tint",hasTint);
        string[] names = hasTint
            ? new[] { "map_base","map_shade","map_fresh","map_dry","map_settings","map_tint" }
            : new[] { "map_base","map_shade","map_fresh","map_dry","map_settings" };
        for (int channel = 0; channel < names.Length; channel++)
        {
            using var image = Image.CreateEmpty(map.Columns,map.Rows,false,Image.Format.Rgbaf);
            for (int y = 0; y < map.Rows; y++) for (int x = 0; x < map.Columns; x++)
            {
                int biomeIndex=map.Regions[map.RegionIdAtCell(x,y)].BiomeIndex;
                var p=compiled[biomeIndex].Base;
                if (p == null) throw new System.ArgumentException("Every streamed biome requires a surface profile.");
                var patches=compiled[biomeIndex].Patches;
                if(patches?.Enabled!=true || patches.Strength<=0) patches=null;
                float strength=patches?.Strength ?? 0f;
                Color value = channel switch { 0 => p.BaseColour, 1 => patches?.ShadeColour ?? p.BaseColour,
                    2 => patches?.FreshColour ?? p.BaseColour, 3 => patches?.DryColour ?? p.BaseColour,
                    5 => compiled[biomeIndex].Tint, _ => new Color(strength,patches?.PatchSize ?? 320f,p.DetailStrength,biomeIndex) };
                image.SetPixel(x,y,value);
            }
            material.SetShaderParameter(names[channel],ImageTexture.CreateFromImage(image));
        }
        material.SetShaderParameter("blend_ground_art",blendArt);
        material.SetShaderParameter("has_surface_styles",hasStyles);
        if(hasStyles)
        {
            using var styles=Image.CreateEmpty(map.Columns,map.Rows,false,Image.Format.Rgbaf);
            for(int y=0;y<map.Rows;y++) for(int x=0;x<map.Columns;x++)
            {
                var p=compiled[map.Regions[map.RegionIdAtCell(x,y)].BiomeIndex].Base;
                styles.SetPixel(x,y,new Color(p.ArtworkInfluence,p.DetailStyle==GroundDetailStyle.Granular ? 1f : 0f,p.DetailStyle==GroundDetailStyle.Mineral ? 1f : 0f,1));
            }
            material.SetShaderParameter("map_styles",ImageTexture.CreateFromImage(styles));
        }
        material.SetShaderParameter("has_ground_overlays",hasOverlays);
        float DefaultSlot(int slot) => compiled.Any(b=>slot<b.Overlays.Length && GroundOverlayPalette.IsVisible(b.Overlays[slot])) ? -1f : 0f;
        material.SetShaderParameter("preview_overlay_visibility",new Vector2(DefaultSlot(0),DefaultSlot(1)));
        if(hasOverlays) GroundOverlayPalette.Bind(material,compiled,overlayArt);
        material.SetShaderParameter("map_origin",map.Bounds.Position); material.SetShaderParameter("map_cell_size",map.CellSize);
        material.SetShaderParameter("map_grid_size",new Vector2(map.Columns,map.Rows)); return material;
    }
    // Each exact clipped chunk rectangle shares the palette and world-space texture coordinates.
    // =========================================================
    public static MeshInstance2D Create(Rect2 bounds, ShaderMaterial material) => new() { Name = "Ground", Mesh = new QuadMesh { Size = bounds.Size }, Position = bounds.GetCenter(), Material = material, ZIndex = -10 };
}
