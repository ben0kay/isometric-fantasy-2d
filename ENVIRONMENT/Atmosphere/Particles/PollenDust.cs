// Sparse screen-space dust: one shader-animated batch, no camera/weather or frame callbacks.
// Legacy Pollen profile fields control this presentation-only overlay; world pollen can be separate later.
using Godot;
public partial class PollenDust : CanvasLayer
{
    private ShaderMaterial _shader;
    private Viewport _viewport;
    private Vector2 _size;
    private int _count=-1;
    public MultiMeshInstance2D Batch { get; private set; }
    public int AppliedUpdates { get; private set; }
    public const int MaximumCount=100;
    // Initialize fixed instance seeds once; draw below colour grading and developer UI.
    // =========================================================
    public override void _Ready()
    {
        Layer=5; FollowViewportEnabled=false;
        _shader=new ShaderMaterial { Shader=GD.Load<Shader>("res://ENVIRONMENT/Atmosphere/Particles/PollenDust.gdshader") };
        Batch=new MultiMeshInstance2D { Material=_shader, Multimesh=new MultiMesh {
            TransformFormat=MultiMesh.TransformFormatEnum.Transform2D, UseCustomData=true,
            Mesh=new QuadMesh { Size=Vector2.One*2 }, InstanceCount=MaximumCount } };
        AddChild(Batch);
        for(int i=0;i<MaximumCount;i++)
        { Batch.Multimesh.SetInstanceTransform2D(i,Transform2D.Identity); Batch.Multimesh.SetInstanceCustomData(i,new Color(i/100f,0,0,1)); }
        _viewport=GetViewport(); _viewport.SizeChanged+=UpdateBounds; UpdateBounds(); SetProcess(false);
    }
    // Profile events change the budget; animation runs entirely through shader TIME.
    // =========================================================
    public void UpdateSettings(int count)
    {
        count=Mathf.Clamp(count,0,MaximumCount);
        if(_count==count) return;
        _count=count; Batch.Multimesh.VisibleInstanceCount=count; AppliedUpdates++;
    }
    // Viewport resize is the only positional input; walking, zoom and scenery never update it.
    // =========================================================
    private void UpdateBounds()
    {
        Vector2 size=_viewport.GetVisibleRect().Size;
        if(_size==size) return;
        _size=size; Batch.Position=size/2;
        _shader.SetShaderParameter("view_size",size);
        Batch.Multimesh.CustomAabb=new Aabb(new Vector3(-size.X/2,-size.Y/2,-1),new Vector3(size.X,size.Y,2));
        AppliedUpdates++;
    }
    public override void _ExitTree() { if(GodotObject.IsInstanceValid(_viewport)) _viewport.SizeChanged-=UpdateBounds; }
}
