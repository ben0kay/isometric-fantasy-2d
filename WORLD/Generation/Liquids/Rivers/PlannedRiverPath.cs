// Frozen river direction/width data for saves and future WG6 consumers; no flow simulation or Resource ownership.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum RiverEndpointKind { SpringPool, TerminalBasin }
public sealed record RiverPathSnapshot(float[] CentrelineXY, float[] Widths, RiverEndpointKind Source, RiverEndpointKind Destination);
public sealed class PlannedRiverPath
{
    public IReadOnlyList<Vector2> Points { get; }
    public IReadOnlyList<float> Widths { get; }
    public RiverEndpointKind Source => RiverEndpointKind.SpringPool;
    public RiverEndpointKind Destination => RiverEndpointKind.TerminalBasin;
    // Keep a bounded simple branch-free route with positive widths and explicit supported terminals.
    // =========================================================
    public PlannedRiverPath(Vector2[] points,float[] widths)
    {
        if (points==null || widths==null || points.Length<2 || points.Length>1000 || points.Length!=widths.Length
            || points.Any(p => !p.IsFinite()) || widths.Any(w => !float.IsFinite(w) || w<32 || w>1024))
            throw new ArgumentException("River needs 2–1000 finite centerline points and positive bounded widths.");
        for (int i=1;i<points.Length;i++)
            if (points[i].DistanceSquaredTo(points[i-1])<1) throw new ArgumentException("River centerline has a zero-length segment.");
        Points=Array.AsReadOnly((Vector2[])points.Clone()); Widths=Array.AsReadOnly((float[])widths.Clone());
    }
    // Detach all arrays before handing values to SAVE or tools.
    // =========================================================
    internal RiverPathSnapshot Snapshot() => new(Points.SelectMany(p => new[] { p.X,p.Y }).ToArray(),Widths.ToArray(),Source,Destination);
    // Restore only supported endpoints and bounded arrays, never route from new-world settings on Continue.
    // =========================================================
    internal static PlannedRiverPath Restore(RiverPathSnapshot value)
    {
        if (value==null) return null;
        if (value.CentrelineXY==null || value.Widths==null || value.CentrelineXY.Length<4 || value.CentrelineXY.Length>2000
            || value.CentrelineXY.Length%2!=0 || value.Widths.Length!=value.CentrelineXY.Length/2
            || value.Source!=RiverEndpointKind.SpringPool || value.Destination!=RiverEndpointKind.TerminalBasin)
            throw new ArgumentException("Invalid frozen river path/endpoints.");
        var points=new Vector2[value.Widths.Length];
        for (int i=0;i<points.Length;i++) points[i]=new(value.CentrelineXY[i*2],value.CentrelineXY[i*2+1]);
        return new PlannedRiverPath(points,value.Widths);
    }
}
