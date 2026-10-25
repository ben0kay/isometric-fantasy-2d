// Tree-only transient falling art. Standing tree physics/environment unregister through normal source teardown.
using Godot;
using System;

public partial class TreeFall : Node2D
{
    private Node2D _pivot;
    private float _age,_seconds,_direction;
    private Action _release;
    private bool _released;
    // Copy rooted art with optional appearance-only material; omit standing colliders, fire, occluders and registrations.
    // =========================================================
    public static TreeFall Begin(FantasyGame.World.Trees.Tree source,Vector2 direction,Action release,bool preserveAppearance=false)
    {
        float seconds=source.Definition.FallSeconds;
        if (!float.IsFinite(seconds) || seconds<0.1f || seconds>3) throw new InvalidOperationException("Invalid tree fall duration.");
        var fall=new TreeFall { _seconds=seconds,_direction=direction.X<0 ? -1 : 1,_release=release,Modulate=source.Modulate };
        source.GetParent().AddChild(fall); fall.GlobalTransform=source.GlobalTransform;
        Vector2 anchor=source.SelectedVariant.GroundOffset;
        fall._pivot=new Node2D { Position=anchor }; fall.AddChild(fall._pivot);
        var art=source.GetNode<Sprite2D>("Sprite");
        ShaderMaterial material=null;
        if(preserveAppearance && art.Material is ShaderMaterial original)
        { material=(ShaderMaterial)original.Duplicate(); material.SetShaderParameter("wind_multiplier",0f); material.SetShaderParameter("strength_world_pixels",0f); }
        fall._pivot.AddChild(new Sprite2D { Texture=art.Texture,Material=material,Centered=art.Centered,Offset=art.Offset,Scale=art.Scale,
            FlipH=art.FlipH,FlipV=art.FlipV,Position=art.Position-anchor,Rotation=art.Rotation,Modulate=art.Modulate,TextureFilter=art.TextureFilter });
        fall.SetPhysicsProcess(false); fall.SetProcess(true); return fall;
    }
    // Fall accelerates away from the impact, releases rewards at ground contact, then fades the transient canopy.
    // =========================================================
    public override void _Process(double delta)
    {
        _age+=(float)delta; float t=Mathf.Clamp(_age/_seconds,0,1);
        _pivot.Rotation=_direction*Mathf.Pi*0.5f*t*t;
        if (t>=1) Release();
        if (_age>_seconds+0.15f) Modulate=new Color(Modulate,Mathf.Clamp(1-(_age-_seconds-0.15f)/0.3f,0,1));
        if (_age>=_seconds+0.45f) { SetProcess(false); QueueFree(); }
    }
    // A fall may be interrupted visually without rolling or reserving its rewards again.
    // =========================================================
    private void Release() { if (_released) return; _released=true; _release?.Invoke(); _release=null; }
    // Scope teardown discards everything; independently removing the proxy resolves its already-reserved outcome.
    // =========================================================
    public override void _ExitTree() { if (GodotObject.IsInstanceValid(GetParent()) && !GetParent().IsQueuedForDeletion()) Release(); }
}
