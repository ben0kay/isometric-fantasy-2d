// Shared conservative artwork bounds for all scenery templates; no per-species processors.
using Godot;
public static class SceneryVisualBounds
{
    // Transform all corners: supports negative scale, offsets, rotation and nested containers.
    // =========================================================
    public static Rect2 Transform(Rect2 rect, Transform2D transform)
    {
        Vector2 a = transform * rect.Position, b = transform * new Vector2(rect.End.X,rect.Position.Y);
        Vector2 c = transform * rect.End, d = transform * new Vector2(rect.Position.X,rect.End.Y);
        Vector2 minimum = a.Min(b).Min(c).Min(d), maximum = a.Max(b).Max(c).Max(d);
        return new Rect2(minimum,maximum-minimum);
    }
    // Use actual template artwork; shader movement is covered by the configurable preparation margin.
    // =========================================================
    public static Rect2 Sprite(Sprite2D sprite) => Transform(sprite.GetRect(),sprite.GlobalTransform);
    // Canvas bounds follow camera clamping, zoom, resizing and rotation rather than player position.
    // =========================================================
    public static Rect2 View(CanvasItem owner) => Transform(owner.GetViewportRect(),owner.GetViewport().GetCanvasTransform().AffineInverse());
    // A chunk owns static scenery, but its artwork can extend well beyond the chunk's ground square.
    // =========================================================
    public static Rect2 Chunk(Node2D chunk)
    {
        Rect2 result = new(); bool first = true;
        foreach (Node child in chunk.GetChildren())
        {
            var sprite = child.GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Texture == null) continue;
            Rect2 bounds = Sprite(sprite); result = first ? bounds : result.Merge(bounds); first = false;
        }
        return result;
    }
}
