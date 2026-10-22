// One bounded GPU-drawn batch replaces per-speck C# trajectories and circle submissions.
// Owned by WorldAtmosphere only while pollen is enabled with a positive count.
using Godot;
public partial class PollenDust : MultiMeshInstance2D
{
    private ShaderMaterial _shader;
    private Rect2 _view;
    private int _count = -1;
    private bool _visible = true;
    private Vector2 _drift = new(float.NaN,float.NaN);
    private float _daylight = float.NaN;
    public int AppliedUpdates { get; private set; }
    public const int MaximumCount = 100;

    // Instance seeds and one tiny quad are initialized once per activation, never per frame.
    // =========================================================
    public override void _Ready()
    {
        _shader = new ShaderMaterial { Shader = GD.Load<Shader>("res://ENVIRONMENT/Atmosphere/Particles/PollenDust.gdshader") };
        Material = _shader;
        Multimesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseCustomData = true, Mesh = new QuadMesh { Size = Vector2.One * 2 }, InstanceCount = MaximumCount };
        for(int i=0;i<MaximumCount;i++)
        {
            Multimesh.SetInstanceTransform2D(i,Transform2D.Identity);
            Multimesh.SetInstanceCustomData(i,new Color(i/100f,0,0,1));
        }
        SetProcess(false);
    }
    // The owner supplies shared view/wind/daylight; no node process, per-speck loop or redraw.
    // =========================================================
    public void UpdateState(int count, Rect2 view, Vector2 drift, float daylight)
    {
        count = Mathf.Clamp(count,0,MaximumCount);
        if (_count != count) { _count = count; Multimesh.VisibleInstanceCount = count; AppliedUpdates++; }
        bool visible = view.HasArea();
        if (_visible != visible) { _visible = visible; Visible = visible; }
        if (_view.Position != view.Position || _view.Size != view.Size) { GlobalPosition = view.GetCenter(); AppliedUpdates++; }
        if (_view.Size != view.Size)
        {
            _shader.SetShaderParameter("view_size",view.Size);
            Multimesh.CustomAabb = new Aabb(new Vector3(-view.Size.X/2,-view.Size.Y/2,-1),new Vector3(view.Size.X,view.Size.Y,2));
            AppliedUpdates++;
        }
        _view = view;
        if (_drift != drift) { _drift = drift; _shader.SetShaderParameter("drift",drift); AppliedUpdates++; }
        if (_daylight != daylight) { _daylight = daylight; _shader.SetShaderParameter("daylight",daylight); AppliedUpdates++; }
    }
}
