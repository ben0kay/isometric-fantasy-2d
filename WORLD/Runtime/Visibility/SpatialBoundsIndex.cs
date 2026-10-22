// World-scoped cached bounds buckets for cosmetic queries; no scene scans or frame callback.
using Godot;
using System.Collections.Generic;
public sealed class SpatialBoundsIndex<T> where T : class
{
    private sealed class Entry { public Rect2 Bounds; public Vector2I Min,Max; public bool Large; }
    private readonly Dictionary<T,Entry> _entries=new();
    private readonly Dictionary<Vector2I,HashSet<T>> _cells=new();
    private readonly HashSet<T> _large=new(), _visited=new();
    private readonly float _cellSize;
    public int Count => _entries.Count;
    public int CellCount => _cells.Count;
    public int LastQueryVisits { get; private set; }
    public SpatialBoundsIndex(float cellSize) { _cellSize=Mathf.Max(1,cellSize); }
    private Vector2I Cell(Vector2 p) => new(Mathf.FloorToInt(p.X/_cellSize),Mathf.FloorToInt(p.Y/_cellSize));
    private static long Area(Vector2I min,Vector2I max) => ((long)max.X-min.X+1)*((long)max.Y-min.Y+1);
    // Updating bounds within the same cells reuses storage, including active hit wobble.
    // =========================================================
    public void Set(T item,Rect2 bounds)
    {
        Vector2I min=Cell(bounds.Position),max=Cell(bounds.End); bool large=Area(min,max)>256;
        if(_entries.TryGetValue(item,out var entry))
        {
            if(entry.Min==min && entry.Max==max) { entry.Bounds=bounds; return; }
            RemoveCells(item,entry);
        }
        else _entries.Add(item,entry=new Entry());
        entry.Bounds=bounds; entry.Min=min; entry.Max=max; entry.Large=large;
        if(large) { _large.Add(item); return; }
        for(int y=min.Y;y<=max.Y;y++) for(int x=min.X;x<=max.X;x++)
        {
            var cell=new Vector2I(x,y);
            if(!_cells.TryGetValue(cell,out var items)) _cells.Add(cell,items=new());
            items.Add(item);
        }
    }
    public void Remove(T item) { if(_entries.Remove(item,out var entry)) RemoveCells(item,entry); _visited.Remove(item); }
    private void RemoveCells(T item,Entry entry)
    {
        if(entry.Large) { _large.Remove(item); return; }
        for(int y=entry.Min.Y;y<=entry.Max.Y;y++) for(int x=entry.Min.X;x<=entry.Max.X;x++)
        {
            var cell=new Vector2I(x,y); var items=_cells[cell]; items.Remove(item); if(items.Count==0) _cells.Remove(cell);
        }
    }
    // Huge artwork/queries use a bounded entry fallback rather than millions of empty cells.
    // Caller owns/reuses its result set; ordinary queries visit only overlapping buckets.
    // =========================================================
    public void Query(Rect2 bounds,HashSet<T> result)
    {
        LastQueryVisits=0; _visited.Clear(); var min=Cell(bounds.Position); var max=Cell(bounds.End);
        if(Area(min,max)>4096) { foreach(var pair in _entries) Test(pair.Key,bounds,result); return; }
        for(int y=min.Y;y<=max.Y;y++) for(int x=min.X;x<=max.X;x++)
            if(_cells.TryGetValue(new Vector2I(x,y),out var items)) foreach(var item in items) Test(item,bounds,result);
        foreach(var item in _large) Test(item,bounds,result);
    }
    private void Test(T item,Rect2 bounds,HashSet<T> result)
    {
        if(!_visited.Add(item)) return;
        LastQueryVisits++; if(_entries[item].Bounds.Intersects(bounds)) result.Add(item);
    }
    public void Clear() { _entries.Clear(); _cells.Clear(); _large.Clear(); _visited.Clear(); }
}
