// One chunk-cropped liquid quad owns its distance texture/material; no processing or physics callbacks.
using Godot;
public partial class LiquidSurfaceTile : MeshInstance2D
{
    public ImageTexture Field { get; set; }
    public Rect2 FieldBounds { get; set; }
    public Rect2 TileBounds { get; set; }
    // Native resources release with this chunk rather than waiting for a world-wide cache or GC.
    // =========================================================
    public override void _ExitTree()
    {
        var material=Material; var mesh=Mesh; Material=null; Mesh=null;
        material?.Dispose(); mesh?.Dispose(); Field?.Dispose(); Field=null;
    }
}
