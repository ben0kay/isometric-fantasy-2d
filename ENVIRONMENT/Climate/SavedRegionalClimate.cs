// Detached coarse visited-cell snow state. Unseen world cells are not simulated or serialized.
using System;
using System.Collections.Generic;
using System.IO;
public sealed record SavedSnowCell(int Key,float Cover,double Updated);
public sealed record SavedRegionalClimate(double Seconds,float Precipitation,bool Enabled,bool ManualOverride,SavedSnowCell[] Cells)
{
    // Validate bounded IDs/timestamps before a SAVE worker accepts or allocates runtime state.
    // =========================================================
    public void Validate(int cellCount)
    {
        if(!double.IsFinite(Seconds) || Seconds<0 || Seconds>1e12 || !float.IsFinite(Precipitation) || Precipitation<0 || Precipitation>3
            || Cells==null || Cells.Length>cellCount || Cells.Length>262144) throw new InvalidDataException("Invalid regional snow state.");
        var keys=new HashSet<int>();
        foreach(var cell in Cells)
            if(cell==null || cell.Key<0 || cell.Key>=cellCount || !keys.Add(cell.Key) || !float.IsFinite(cell.Cover) || cell.Cover<0 || cell.Cover>1
                || !double.IsFinite(cell.Updated) || cell.Updated<0 || cell.Updated>Seconds) throw new InvalidDataException("Invalid regional snow cell.");
    }
}
