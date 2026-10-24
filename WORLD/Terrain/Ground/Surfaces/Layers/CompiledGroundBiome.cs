// Setup-only resolved references; completed render maps never retain mutable biome stacks.
using Godot;
using System;
public sealed class CompiledGroundBiome
{
    public GroundSurfaceProfile Base { get; }
    public Color Tint { get; }
    public GroundPatchLayer Patches { get; }
    public BiomeGroundLayer[] Overlays { get; }

    // Resolve explicit stacks first; the old Surface property is only a legacy fallback.
    // =========================================================
    public CompiledGroundBiome(WorldBiomeDefinition biome)
    {
        if (biome.GroundStack is { } stack)
        {
            Base=stack.ResolveBaseSurface(); Tint=biome.GroundTint; Patches=stack.AccompanyingPatches;
            Overlays=new BiomeGroundLayer[stack.OverlayLayers.Count];
            for(int i=0;i<Overlays.Length;i++) Overlays[i]=stack.OverlayLayers[i];
        }
        else
        {
            Base=biome.Surface ?? throw new ArgumentException("A streamed biome requires base ground.");
            Base.Validate(); Tint=biome.GroundTint; Patches=GroundPatchLayer.FromLegacy(Base); Overlays=Array.Empty<BiomeGroundLayer>();
        }
        if(Base.Recipe!=GroundMaterialRecipe.Artwork || Base.Texture==null)
            throw new ArgumentException("Base ground requires artwork; procedural recipes are supported on extra patch layers.");
        RequireSupportedMaterial(Base);
        foreach(var layer in Overlays) RequireSupportedMaterial(layer.Surface);
    }
    // Streamed composition supports these data styles, not arbitrary custom ShaderMaterial code.
    // =========================================================
    private static void RequireSupportedMaterial(GroundSurfaceProfile profile)
    {
        const string shared="res://WORLD/Terrain/Ground/Surfaces/Shaders/GrassSurface.gdshader";
        var shader=profile.Material.Shader;
        if(shader==null || (shader.ResourcePath!=shared && shader.Code!=GD.Load<Shader>(shared).Code))
            throw new ArgumentException("Streamed surfaces require the shared ground material; select a supported DetailStyle for different surface families.");
    }
}
