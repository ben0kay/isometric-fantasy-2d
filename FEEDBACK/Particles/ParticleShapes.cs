// Small shared immutable draw geometry; no per-particle arrays or visual-resource duplication.
using Godot;
public static class ParticleShapes
{
    private static readonly Vector2[] Leaf = { new(-2,0),new(0,-1),new(2,0),new(0,1) };
    private static readonly Vector2[] Shard = { new(-2,1),new(-1,-2),new(2,0) };
    // Geometry uses the caller's current draw transform, including size, rotation and stretch.
    // =========================================================
    public static void Draw(Node2D canvas,ParticleShape shape,Color colour)
    {
        switch(shape)
        {
            case ParticleShape.Shard: canvas.DrawColoredPolygon(Shard,colour); break;
            case ParticleShape.Droplet: canvas.DrawCircle(Vector2.Zero,1.5f,colour); break;
            case ParticleShape.Spark: canvas.DrawLine(new(-2,0),new(2,0),colour,1); break;
            default: canvas.DrawColoredPolygon(Leaf,colour); break;
        }
    }
}
