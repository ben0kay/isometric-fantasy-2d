// GS2 base-map parity against frozen setup plus GS3 packing/determinism/zero-layer checks.
using Godot;
using System;
using System.Linq;
public partial class ValidateGroundLayers : Node
{
    private int _checks;
    // Run independently of the practice room and never mutate authored production Resources.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"GS2/GS3 PASS: {_checks} checks"); GetTree().Quit(); }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Original profiles still produce byte-identical coarse maps, artwork and sampling parameters.
    // =========================================================
    private void Run()
    {
        var config=GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres");
        foreach(int seed in new[] {213876,-42})
        {
            var map=MacroWorldBuilder.Build(config,seed);
            using var old=PreGS2GroundPalette.Build(map,config.Biomes);
            using var current=StreamedGroundPalette.Build(map,config.Biomes);
            foreach(string name in new[] {"ground_art","map_base","map_shade","map_fresh","map_dry","map_settings"})
                SameTexture(old,current,name);
            foreach(string name in new[] {"has_ground_tint","has_ground_overlays","has_surface_styles","blend_ground_art"})
                Check(!current.GetShaderParameter(name).AsBool(),"Original production fast path: "+name);
            Check(map.Hash==MacroWorldBuilder.Build(config,seed).Hash,"Geography stable");
        }
        var fixture=GroundLayerFixture.Definitions(); var f=GroundLayerFixture.Map();
        using var both=StreamedGroundPalette.Build(f,fixture);
        using var repeat=StreamedGroundPalette.Build(f,fixture.Reverse());
        Check(both.GetShaderParameter("has_ground_overlays").AsBool(),"Enabled overlays render");
        Check(both.GetShaderParameter("blend_ground_art").AsBool(),"Different base artwork blends");
        Check(both.GetShaderParameter("has_surface_styles").AsBool(),"Non-grass family supported");
        SameTexture(both,repeat,"overlay_table"); SameTexture(both,repeat,"ground_art");
        using var table=Texture(both,"overlay_table").GetImage();
        Check(table.GetWidth()==9 && table.GetHeight()==4,"Bounded table dimensions");
        Check(table.GetPixel(8,0).G==1 && table.GetPixel(8,1).G==1,"A and B active in authored order");
        Check(table.GetPixel(8,2).G==0 && table.GetPixel(8,3).G==0,"Unconfigured biome stays clear");
        Check(table.GetPixel(7,0)!=table.GetPixel(7,1),"Independent mask salts");
        var layers=fixture[0].GroundStack.OverlayLayers;
        layers[0].Enabled=false;
        using var onlyB=StreamedGroundPalette.Build(f,fixture);
        using var b=Texture(onlyB,"overlay_table").GetImage();
        Check(b.GetPixel(8,0).G==0 && b.GetPixel(8,1).G==1,"A off leaves B in original slot");
        layers[0].Enabled=true; layers[1].Enabled=false;
        using var onlyA=StreamedGroundPalette.Build(f,fixture);
        using var a=Texture(onlyA,"overlay_table").GetImage();
        Check(a.GetPixel(8,0).G==1 && a.GetPixel(8,1).G==0,"B off leaves A");
        layers[0].Coverage=0; layers[1].Enabled=true; layers[1].Opacity=0;
        using var zero=StreamedGroundPalette.Build(f,fixture);
        Check(!zero.GetShaderParameter("has_ground_overlays").AsBool(),"Zero coverage/opacity skip overlay table");
        Check(GD.Load<WorldBiomeDefinition>("res://TESTS/Terrain/Fixtures/SandBiome.tres").GroundStack.OverlayLayers[0].Enabled,"Fixture edits did not mutate authored source");
        using var left=StreamedGroundPalette.Create(new Rect2(-512,-512,512,512),both);
        using var right=StreamedGroundPalette.Create(new Rect2(0,-512,512,512),both);
        Check(left.Material==right.Material,"Adjacent chunk quads share one material");
        Check(left.Position.X+256==right.Position.X-256,"Chunk clipping meets exactly");
        layers[0].Coverage=0.6f; layers[1].Opacity=0.65f;
        using var reload=StreamedGroundPalette.Build(f,fixture);
        SameTexture(both,reload,"overlay_table"); SameTexture(both,reload,"ground_art");
    }
    // Byte comparison detects changed atlas order, palette fields or mask packing on reload.
    // =========================================================
    private void SameTexture(ShaderMaterial a,ShaderMaterial b,string name)
    {
        using var first=Texture(a,name).GetImage(); using var second=Texture(b,name).GetImage();
        Check(first.GetWidth()==second.GetWidth() && first.GetHeight()==second.GetHeight()
            && first.GetData().SequenceEqual(second.GetData()),"Texture parity: "+name);
    }
    // Read generated texture parameters without touching authored Resources.
    // =========================================================
    private static Texture2D Texture(ShaderMaterial material,string name) => (Texture2D)material.GetShaderParameter(name).AsGodotObject();
    // A failed invariant returns a nonzero suite exit code.
    // =========================================================
    private void Check(bool value,string label) { if(!value) throw new Exception(label); _checks++; }
}
