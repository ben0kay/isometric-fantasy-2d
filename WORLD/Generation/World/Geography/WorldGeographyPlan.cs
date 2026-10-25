// Immutable major geography independent of biome IDs. Existing water supplies the first provider; no runtime callbacks.
using Godot;
using System;

[Flags]
public enum GeographyWaterKind : byte { None=0, Lake=1, River=2 }
public sealed class WorldGeographyPlan
{
    public const int Version=1;
    public int Seed { get; }
    public Rect2 Bounds { get; }
    public WaterBodyPlan Water { get; }
    // Reuse frozen canonical water geometry, rather than retaining authoring Resources or duplicating polygons.
    // =========================================================
    public WorldGeographyPlan(int seed,Rect2 bounds,WaterBodyPlan water)
    {
        if (!bounds.Position.IsFinite() || !bounds.Size.IsFinite() || !bounds.End.IsFinite() || bounds.Size.X<=0 || bounds.Size.Y<=0
            || water==null || water.Seed!=seed || water.Bounds!=bounds) throw new ArgumentException("Geography requires finite bounds and matching water ownership.");
        foreach (var body in water.Bodies) if (!bounds.Encloses(body.Bounds)) throw new ArgumentException("Geography escaped its world bounds.");
        Seed=seed; Bounds=bounds; Water=water;
    }
    // Conservative cell footprint queries protect even narrow banks crossing a coarse biome cell.
    // =========================================================
    public GeographyWaterKind WaterAt(Rect2 area)
    {
        GeographyWaterKind result=GeographyWaterKind.None;
        foreach (var body in Water.Bodies)
            if (body.Intersects(area)) result|=body.River==null ? GeographyWaterKind.Lake : GeographyWaterKind.River;
        return result;
    }
    // One byte per macro cell, built once for biome assignment and released when that planner returns.
    // =========================================================
    internal GeographyWaterKind[] BuildWaterMask(float cellSize,int columns,int rows)
    {
        if (!float.IsFinite(cellSize) || cellSize<=0 || columns<1 || rows<1 || (long)columns*rows>4194304
            || Math.Ceiling((double)Bounds.Size.X/cellSize)!=columns || Math.Ceiling((double)Bounds.Size.Y/cellSize)!=rows)
            throw new ArgumentException("Geography mask dimensions do not match the world.");
        var mask=new GeographyWaterKind[columns*rows];
        foreach (var body in Water.Bodies)
        {
            int left=Math.Max(0,Mathf.FloorToInt((body.Bounds.Position.X-Bounds.Position.X)/cellSize)-1);
            int right=Math.Min(columns-1,Mathf.FloorToInt((body.Bounds.End.X-Bounds.Position.X)/cellSize));
            int top=Math.Max(0,Mathf.FloorToInt((body.Bounds.Position.Y-Bounds.Position.Y)/cellSize)-1);
            int bottom=Math.Min(rows-1,Mathf.FloorToInt((body.Bounds.End.Y-Bounds.Position.Y)/cellSize));
            for (int y=top;y<=bottom;y++) for (int x=left;x<=right;x++)
            {
                var cell=new Rect2(Bounds.Position+new Vector2(x,y)*cellSize,Vector2.One*cellSize).Intersection(Bounds);
                if (body.Intersects(cell)) mask[y*columns+x]|=body.River==null ? GeographyWaterKind.Lake : GeographyWaterKind.River;
            }
        }
        return mask;
    }
}
