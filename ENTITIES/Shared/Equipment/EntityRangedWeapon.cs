// Optional enemy bow canvas; the existing motor updates it, with no separate process callbacks.
using Godot;
public partial class EntityRangedWeapon : Node2D
{
    private EntityEquipmentDefinition _definition;
    private float _draw=-1;
    // Equipment definition and artwork remain with their owning enemy variant.
    // =========================================================
    public void Configure(EntityEquipmentDefinition definition)
    { _definition=definition; TextureFilter=TextureFilterEnum.Nearest; SetProcess(false); SetPhysicsProcess(false); QueueRedraw(); }
    // Stable poses change no draw data; active draw-back remains readable during wind-up.
    // =========================================================
    public void Pose(Vector2 aim,float draw)
    {
        if(_definition?.Sprite==null) { Hide(); return; }
        Show();
        Vector2 position=_definition.HandOffset+aim*Mathf.Clamp(_definition.HoldDistance,0,24);
        if(Position!=position) Position=position;
        float angle=aim.Angle(); if(!Mathf.IsEqualApprox(Rotation,angle)) Rotation=angle;
        Vector2 scale=_definition.SpriteScale.IsFinite() ? _definition.SpriteScale.Clamp(Vector2.One*0.1f,Vector2.One*3) : Vector2.One;
        if(Scale!=scale) Scale=scale;
        ZIndex=aim.Y<0 ? -1 : 1;
        draw=Mathf.Clamp(draw,0,1);
        if(Mathf.Abs(draw-_draw)<0.01f) return;
        _draw=draw; QueueRedraw();
    }
    // Sprite, bowstring and nocked arrow are presentation only; world-space shots come from R0.
    // =========================================================
    public override void _Draw()
    {
        if(_definition?.Sprite==null) return;
        DrawSetTransform(_definition.GripOffset);
        Vector2 size=_definition.Sprite.GetSize(),origin=-size*0.5f;
        DrawTextureRect(_definition.Sprite,new Rect2(origin,size),false);
        Vector2 pull=new(-4-Mathf.Max(0,_draw)*6,0);
        DrawLine(new Vector2(-4,-13),pull,new Color(0.75f,0.73f,0.65f),1);
        DrawLine(pull,new Vector2(-4,13),new Color(0.75f,0.73f,0.65f),1);
        if(_draw>0) DrawLine(pull,pull+Vector2.Right*16,new Color(0.9f,0.85f,0.65f),1);
    }
}
