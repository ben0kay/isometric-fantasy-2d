// Value-only fixed-pond geometry shared by runtime and metadata previews; never creates surfaces or effects.
using Godot;
public static class WorldLiquidFootprint
{
    // Preserve the shipped pond's exact harmonic polygon and vertex order.
    // =========================================================
    public static Vector2[] Boundary(WorldLiquidMasterConfig config)
    {
        var polygon=new Vector2[96];
        float phase=(config.ShapeSeed%997)*0.01f;
        Vector2 radius=config.Radius.Abs().Max(Vector2.One*40f);
        for(int i=0;i<polygon.Length;i++)
        {
            float angle=Mathf.Tau*i/polygon.Length;
            float scale=1f+0.12f*Mathf.Sin(angle*3f+phase)+0.065f*Mathf.Cos(angle*5f-phase)+0.035f*Mathf.Sin(angle*7f);
            polygon[i]=Vector2.FromAngle(angle)*radius*scale;
        }
        return polygon;
    }
    // Same bank-inclusive local bounds as WorldLiquidRegion, usable without instantiating its scene.
    // =========================================================
    public static Rect2 LocalBounds(Vector2[] polygon)
    {
        var bounds=new Rect2(polygon[0],Vector2.Zero);
        foreach(var point in polygon) bounds=bounds.Expand(point);
        return bounds.Grow(40f);
    }
    // The production pond roots have identity transforms; callers with moved roots must transform these bounds.
    // =========================================================
    public static Rect2 WorldBounds(WorldLiquidMasterConfig config)
    {
        var bounds=LocalBounds(Boundary(config));
        return new Rect2(bounds.Position+config.Centre,(bounds.End+config.Centre)-(bounds.Position+config.Centre)).Abs();
    }
}
