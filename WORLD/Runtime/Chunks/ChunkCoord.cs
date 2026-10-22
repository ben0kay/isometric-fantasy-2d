// Logical surface chunk coordinates use mathematical floor, including negative world positions.
using Godot;
public readonly record struct ChunkCoord(int X, int Y)
{
    public static ChunkCoord At(Vector2 p, int size) => new(Mathf.FloorToInt(p.X / size), Mathf.FloorToInt(p.Y / size));
    public Rect2 Bounds(int size) => new(X * size, Y * size, size, size);
    public int Distance(ChunkCoord other) => System.Math.Max(System.Math.Abs(X - other.X), System.Math.Abs(Y - other.Y));
}
public enum WorldChunkState { Queued, GeneratingData, ApplyingScene, Loaded, Unloading }
