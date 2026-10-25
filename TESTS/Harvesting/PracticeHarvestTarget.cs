// Generic disposable work target proves harvesting/rewards do not depend on tree or species logic.
using Godot;
public partial class PracticeHarvestTarget : StaticBody2D, IToolHitTarget, IHarvestHitTarget
{
    public Harvestable State { get; private set; }
    public Vector2 HitAnchor => GlobalPosition;
    // Compose the same extraction component and reward coordinator used by trees.
    // =========================================================
    public void Initialize(HarvestProfile profile,WorldDropSpawner drops,string sourceId)
    {
        AddToGroup("world_tool_targets");
        AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(24,24) } });
        State=new Harvestable(); State.Initialize(profile,drops.Catalog,drops.ScopeId+"/"+sourceId+"/extract/0"); AddChild(State);
        State.TryComplete=(rewards,hit)=>
        {
            if (!drops.TryReserve(State.EventId,GlobalPosition,rewards,this)) return false;
            drops.Release(State.EventId); QueueFree(); return true;
        };
        State.Changed+=QueueRedraw; QueueRedraw();
    }
    // Expose real feet geometry to the existing confirmed melee query.
    // =========================================================
    public Vector2 ClosestHitPoint(Vector2 origin) => GlobalPosition+(origin-GlobalPosition).Clamp(new Vector2(-12,-12),new Vector2(12,12));
    // Legacy hits are cosmetic and never generate extraction work.
    // =========================================================
    public void ReceiveHit(Vector2 direction) { }
    // Only the enriched authoritative contact can advance work.
    // =========================================================
    public void ReceiveHarvestHit(HarvestHit hit) => State.Apply(hit);
    // Static card illustrates progress; redraw wakes only on an accepted work change.
    // =========================================================
    public override void _Draw()
    {
        DrawRect(new Rect2(-14,-20,28,32),new Color("#92704d"));
        if (State!=null) DrawRect(new Rect2(-14,14,28*State.Progress/State.RequiredWork,3),new Color("#d8bf84"));
    }
}
