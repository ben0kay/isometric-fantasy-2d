// Creation-only preflight for the bounded coarse skeleton + stateless local refinement architecture.
using Godot;
using System;
public static class WorldMacroBudget
{
    // Retain existing SAVE cell/region limits and reject coordinate magnitudes needing a floating-origin phase.
    // =========================================================
    public static void Validate(WorldGenerationMasterConfig config)
    {
        if(!config.ScalableMacro) return;
        var b=config.Bounds;
        if(config.Authoring==null || !b.Position.IsFinite() || !b.Size.IsFinite() || !b.End.IsFinite() || b.Size.X<=0 || b.Size.Y<=0
            || !float.IsFinite(config.CellSize) || config.CellSize<512 || config.CellSize>8192
            || Math.Max(Math.Max(Math.Abs(b.Position.X),Math.Abs(b.End.X)),Math.Max(Math.Abs(b.Position.Y),Math.Abs(b.End.Y)))>524288
            || config.MaximumCells<1 || config.MaximumCells>262144 || config.MaximumRegions<1 || config.MaximumRegions>4096
            || !float.IsFinite(config.RegionDiameterScale) || config.RegionDiameterScale<1 || config.RegionDiameterScale>16
            || !float.IsFinite(config.RefinementStrength) || config.RefinementStrength<0 || config.RefinementStrength>0.25f)
            throw new ArgumentException("BC4 macro preset exceeds supported resolution, region or float-coordinate budget.");
        long cells=checked((long)Math.Ceiling(b.Size.X/config.CellSize)*(long)Math.Ceiling(b.Size.Y/config.CellSize));
        if(cells>config.MaximumCells || cells<1 || config.Biomes==null || config.Biomes.Count>256
            || cells*config.Biomes.Count>16777216) throw new ArgumentException("BC4 coarse map/climate cache budget exceeded.");
        // Seven shared RGBA32F palette maps, not a world-sized scene; explicit tuning changes require a new budget review.
        if(cells*7*16>32L*1024*1024) throw new ArgumentException("BC4 shared ground palette estimate exceeds 32 MiB.");
    }
}
