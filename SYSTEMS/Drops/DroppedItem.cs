// Real world pickup presentation: grounded root/shadow, temporary hop/tumble on the artwork, dormant after landing.
using Godot;

// Logical data contains no Nodes; H4 will persist/chunk these records before enabling production.
public readonly record struct WorldDropRecord(string Id,string EventId,string ItemId,int Quantity,Vector2 Origin,Vector2 Landing,float LandingRotation,float StartRotation,bool Released=false,bool Collected=false,string SourceId="");
public partial class DroppedItem : Node2D
{
    private const float RestingArtworkY=-7f;
    public string DropId { get; private set; }
    public bool Settled { get; private set; }
    private Sprite2D _art;
    private WorldDropRecord _record;
    private DropScatterValues _settings;
    private float _elapsed,_bounceVariation;
    // Construct art once; hidden reserved drops carry no active animation.
    // =========================================================
    public void Configure(WorldDropRecord record, ItemDefinition item, DropScatterValues settings)
    {
        DropId=record.Id; _record=record; _settings=settings;
        // Small deterministic variation prevents every log in the same burst bouncing to exactly the same height.
        _bounceVariation=0.8f+0.4f*(DropRandom.Hash(record.Id)%1024)/1023f;
        _art=GetNode<Sprite2D>("Artwork"); _art.Texture=item.WorldSprite ?? item.Icon;
        if (_art.Texture!=null) _art.Scale=Vector2.One*(item.WorldSize/Mathf.Max(_art.Texture.GetWidth(),_art.Texture.GetHeight()));
        GlobalPosition=record.Origin; Hide(); SetProcess(false); SetPhysicsProcess(false);
    }
    // Releasing a reserved logical batch wakes presentation once.
    // =========================================================
    public void Release()
    { if (Visible || Settled) return; Show(); _elapsed=0; SetProcess(true); Animate(0); }
    // Stream/save restoration resolves cosmetics to their committed landing, never replays collection or rewards.
    // =========================================================
    public void RestoreSettled()
    { Show(); Animate(1); Settled=true; SetProcess(false); SetPhysicsProcess(false); }
    // Only airborne drops update; collection uses an explicit nearby request, never item polling.
    // =========================================================
    public override void _Process(double delta)
    {
        float bounceTime=_settings.LandingBounceCount>0 && _settings.LandingBounceHeight>0 ? _settings.LandingBounceSeconds : 0;
        float total=_settings.FlightSeconds+bounceTime;
        _elapsed=Mathf.Min(total,_elapsed+Mathf.Max((float)delta,0));
        if(_elapsed<=_settings.FlightSeconds || bounceTime==0) Animate(Mathf.Min(1,_elapsed/_settings.FlightSeconds));
        else { Animate(1); AnimateLandingBounce((_elapsed-_settings.FlightSeconds)/bounceTime); }
        if(_elapsed>=total) { Animate(1); Settled=true; SetProcess(false); }
    }
    // Ground motion and visual height remain distinct in the top-down coordinate space.
    // =========================================================
    private void Animate(float t)
    {
        GlobalPosition=_record.Origin.Lerp(_record.Landing,t);
        _art.Position=new Vector2(0,RestingArtworkY-4*_settings.HopHeight*t*(1-t));
        _art.Rotation=Mathf.Lerp(_record.StartRotation,_record.LandingRotation,t);
    }
    // Brief diminishing post-contact hops; the grounded root and authoritative landing never move again.
    // =========================================================
    private void AnimateLandingBounce(float t)
    {
        float scaled=Mathf.Clamp(t,0,1)*_settings.LandingBounceCount;
        int hop=Mathf.Min(Mathf.FloorToInt(scaled),_settings.LandingBounceCount-1);
        float phase=Mathf.Clamp(scaled-hop,0,1);
        float height=_settings.LandingBounceHeight*_bounceVariation*Mathf.Pow(0.4f,hop);
        _art.Position=new Vector2(0,RestingArtworkY-4*height*phase*(1-phase));
    }
    // Static grounded shadow redraws only when the scene first appears.
    // =========================================================
    public override void _Draw() => DrawEllipse();
    // Shared simple ground mark needs no shader/material instance or physics body.
    // =========================================================
    private void DrawEllipse()
    { DrawSetTransform(Vector2.Zero,0,new Vector2(1,0.4f)); DrawCircle(Vector2.Zero,8,new Color(0.03f,0.04f,0.03f,0.4f)); }
}
