// Bounded deterministic site neighbourhoods replace all-prior-site scans only in the BC4 generator.
using System;
using System.Collections.Generic;
internal sealed class MacroSiteIndex
{
    private readonly int _columns;
    private readonly float _binSize,_cellSize;
    private readonly Dictionary<(int X,int Y),List<(int Cell,float Diameter)>> _bins=new();
    // Nine bins with at most sixteen retained representatives each bound candidate work independently of site count.
    // =========================================================
    internal MacroSiteIndex(int columns,float cellSize,float meanDiameter)
    { _columns=columns; _cellSize=cellSize; _binSize=Math.Max(1,meanDiameter/cellSize); }
    internal void Add(int cell,float diameter)
    {
        var key=Key(cell);
        if(!_bins.TryGetValue(key,out var bin)) _bins.Add(key,bin=new());
        if(bin.Count<16) bin.Add((cell,diameter));
    }
    // Approximate nearest score is an aesthetic spreading heuristic, never a collision/eligibility guarantee.
    // =========================================================
    internal double Score(int cell,float diameter)
    {
        var key=Key(cell); double nearest=double.PositiveInfinity;
        for(int y=key.Y-1;y<=key.Y+1;y++) for(int x=key.X-1;x<=key.X+1;x++)
        {
            if(!_bins.TryGetValue((x,y),out var bin)) continue;
            foreach(var other in bin)
            {
                double dx=cell%_columns-other.Cell%_columns,dy=cell/_columns-other.Cell/_columns;
                double scale=(diameter+other.Diameter)/_cellSize;
                nearest=Math.Min(nearest,(dx*dx+dy*dy)/(scale*scale));
            }
        }
        return nearest;
    }
    private (int X,int Y) Key(int cell) => ((int)(cell%_columns/_binSize),(int)(cell/_columns/_binSize));
}
