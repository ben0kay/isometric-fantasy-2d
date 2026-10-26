// Immutable shoreline acceleration: bounded BVH nearest/overlap queries and half-open Y buckets for containment.
using Godot;
using System;
using System.Collections.Generic;
internal sealed class WaterEdgeIndex
{
    private readonly Vector2[] _points,_delta;
    private readonly Rect2[] _boxes;
    private readonly float[] _inverse;
    private readonly int[] _order;
    private readonly Node[] _nodes;
    private readonly List<int>[] _rows=new List<int>[64];
    private readonly Rect2 _bounds;
    private readonly struct Node
    {
        public readonly Rect2 Bounds; public readonly int Left,Right,Start,Count;
        public Node(Rect2 bounds,int left,int right,int start,int count) { Bounds=bounds; Left=left; Right=right; Start=start; Count=count; }
    }
    // Construction scales with polygon complexity, never world area; original geometry/order remains untouched.
    // =========================================================
    internal WaterEdgeIndex(Vector2[] points,Rect2[] boxes,Vector2[] delta,float[] inverse)
    {
        _points=points; _boxes=boxes; _delta=delta; _inverse=inverse;
        _bounds=FeatureGeometry.Bounds(points); _order=new int[points.Length];
        for(int i=0;i<_order.Length;i++) _order[i]=i;
        var nodes=new List<Node>(); Build(0,points.Length,nodes); _nodes=nodes.ToArray();
        for(int i=0;i<_rows.Length;i++) _rows[i]=new();
        for(int i=0;i<points.Length;i++)
        {
            int first=Row(boxes[i].Position.Y),last=Row(boxes[i].End.Y);
            for(int row=first;row<=last;row++) _rows[row].Add(i);
        }
    }
    // Median splitting caps tree depth; leaves retain at most eight exact segment tests.
    // =========================================================
    private int Build(int start,int count,List<Node> nodes)
    {
        Rect2 bounds=_boxes[_order[start]];
        for(int i=start+1;i<start+count;i++) bounds=bounds.Merge(_boxes[_order[i]]);
        int id=nodes.Count; nodes.Add(default);
        if(count<=8) { nodes[id]=new(bounds.Grow(0.125f),-1,-1,start,count); return id; }
        bool horizontal=bounds.Size.X>=bounds.Size.Y;
        Array.Sort(_order,start,count,Comparer<int>.Create((a,b)=>
        {
            float aa=horizontal ? _boxes[a].GetCenter().X : _boxes[a].GetCenter().Y;
            float bb=horizontal ? _boxes[b].GetCenter().X : _boxes[b].GetCenter().Y;
            int comparison=aa.CompareTo(bb); return comparison!=0 ? comparison : a.CompareTo(b);
        }));
        int half=count/2,left=Build(start,half,nodes),right=Build(start+half,count-half,nodes);
        nodes[id]=new(bounds.Grow(0.125f),left,right,0,0); return id;
    }
    // Double-precision ray crossing preserves the original half-open endpoint convention.
    // =========================================================
    internal bool Contains(Vector2 point)
    {
        var envelope=_bounds.Grow(0.125f);
        if(point.Y<envelope.Position.Y || point.Y>envelope.End.Y || point.X<envelope.Position.X || point.X>envelope.End.X) return false;
        bool inside=false;
        foreach(int i in _rows[Row(point.Y)])
        {
            Vector2 a=_points[i],b=_points[(i+1)%_points.Length];
            if((a.Y>point.Y)!=(b.Y>point.Y) && point.X<(double)a.X+((double)b.X-a.X)*(point.Y-a.Y)/((double)b.Y-a.Y)) inside=!inside;
        }
        return inside;
    }
    internal float NearestSquared(Vector2 point) => Nearest(0,point,float.MaxValue);
    // No query allocations, mutable cache or query-order dependence.
    // =========================================================
    private float Nearest(int id,Vector2 point,float best)
    {
        var node=_nodes[id]; if(BoxDistance(node.Bounds,point)>best) return best;
        if(node.Count>0)
        {
            for(int j=node.Start;j<node.Start+node.Count;j++)
            {
                int i=_order[j]; if(BoxDistance(_boxes[i],point)>best) continue;
                float t=Mathf.Clamp((point-_points[i]).Dot(_delta[i])*_inverse[i],0,1);
                best=Mathf.Min(best,point.DistanceSquaredTo(_points[i]+_delta[i]*t));
            }
            return best;
        }
        int a=node.Left,b=node.Right;
        if(BoxDistance(_nodes[a].Bounds,point)>BoxDistance(_nodes[b].Bounds,point)) (a,b)=(b,a);
        return Nearest(b,point,Nearest(a,point,best));
    }
    internal bool Touches(Rect2 area,float margin) => Touches(0,area,margin);
    // Preserve conservative edge-box overlap semantics used by streaming and footprint checks.
    // =========================================================
    private bool Touches(int id,Rect2 area,float margin)
    {
        var node=_nodes[id]; if(!node.Bounds.Grow(margin).Intersects(area,true)) return false;
        if(node.Count==0) return Touches(node.Left,area,margin) || Touches(node.Right,area,margin);
        for(int i=node.Start;i<node.Start+node.Count;i++) if(_boxes[_order[i]].Grow(margin).Intersects(area,true)) return true;
        return false;
    }
    private int Row(float y) => Math.Clamp(Mathf.FloorToInt((y-_bounds.Position.Y)/Mathf.Max(_bounds.Size.Y,0.001f)*64),0,63);
    private static float BoxDistance(Rect2 box,Vector2 point) => point.DistanceSquaredTo(point.Clamp(box.Position,box.End));
}
