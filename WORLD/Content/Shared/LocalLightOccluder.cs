// Object-owned native light blocker; derives a footprint once, never raycasts or ticks.
using Godot;
public partial class LocalLightOccluder : LightOccluder2D
{
    [Export] public NodePath FootprintPath { get; set; } = "../TrunkFootprint";
    [Export] public Vector2[] Outline { get; set; } = System.Array.Empty<Vector2>();
    // Final configured/mirrored collision is available before this child becomes ready.
    // Separate authored Outline data can replace the footprint without changing physics.
    // =========================================================
    public override void _Ready()
    {
        Vector2[] polygon = Outline;
        if (polygon.Length < 3 && GetNodeOrNull<CollisionPolygon2D>(FootprintPath) is CollisionPolygon2D footprint)
        {
            polygon = (Vector2[])footprint.Polygon.Clone();
            for (int i = 0; i < polygon.Length; i++) polygon[i] = ToLocal(footprint.ToGlobal(polygon[i]));
        }
        Occluder = polygon.Length < 3 ? null : new OccluderPolygon2D {
            Polygon = polygon, Closed = true, CullMode = OccluderPolygon2D.CullModeEnum.Disabled
        };
        SetProcess(false); SetPhysicsProcess(false);
    }
}
