// One transient straight arrow: swept point collision, one canonical impact, then immediate retirement.
using Godot;
using System;
public enum ProjectileEnd { Impact, Expired, Boundary, SourceLost, Cancelled }
public partial class Projectile : Node2D
{
    private ProjectileSpawner _spawner;
    private ICombatTarget _source;
    private CombatFaction _faction;
    private ProjectileValues _values;
    private Vector2 _direction;
    private PhysicsRayQueryParameters2D _ray;
    private float _age,_distance;
    private bool _active,_released;
    public bool Active => _active;
    public int QueryCount { get; private set; }
    public event Action<ProjectileEnd,AttackContact?> Finished;
    // Configure only through the budget-owning spawner; exclude the source body once at launch.
    // =========================================================
    internal void Configure(ProjectileSpawner spawner,ICombatTarget source,ProjectileValues values,Vector2 direction)
    {
        _spawner=spawner; _source=source; _faction=source.Faction; _values=values; _direction=direction;
        _ray=new PhysicsRayQueryParameters2D { CollisionMask=3,CollideWithBodies=true,CollideWithAreas=false,HitFromInside=true,
            Exclude=new Godot.Collections.Array<Rid> { ((CollisionObject2D)source.CombatNode).GetRid() } };
        _active=true;
    }
    // The arrow is drawn once; moving/rotating the canvas needs no per-frame redraw requests.
    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(_active); QueueRedraw(); }
    // Inspect the full travelled segment, including the final partial lifetime/range step.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var trace=RuntimeFrameTrace.Measure("Projectile.PhysicsProcess");
        if(!_active) return;
        if(!CombatTargeting.Active(_source)) { Retire(ProjectileEnd.SourceLost); return; }
        if(!double.IsFinite(delta) || delta<=0) return;
        float dt=(float)Math.Min(delta,_values.Lifetime-_age);
        float travel=Mathf.Min(_values.Speed*dt,_values.Distance-_distance);
        Vector2 from=GlobalPosition,to=from+_direction*travel;
        if(!_spawner.AllowsSegment(from,to)) { Retire(ProjectileEnd.Boundary); return; }
        _ray.From=from; _ray.To=to;
        var hit=GetWorld2D().DirectSpaceState.IntersectRay(_ray); QueryCount++;
        if(hit.Count!=0)
        {
            Vector2 point=hit["position"].AsVector2(); GlobalPosition=point;
            // Disable before callbacks/damage: re-entry can never produce a second hit.
            _active=false; SetPhysicsProcess(false); Hide();
            AttackContact? contact=null;
            if(hit["collider"].AsGodotObject() is ICombatTarget target && CombatTargeting.Active(target)
                && target.Faction!=_faction && (target.Faction&_values.Impact.Targets)!=0)
            {
                // Incoming bearing, not a moving shooter's current position, drives directional guard.
                var damage=target.ReceiveDamage(new DamageRequest(_values.Impact.Damage,target.HitAnchor-_direction*128),_values.Stats);
                contact=new AttackContact(target.CombatNode,damage,_source.CombatNode,point,_direction);
                if(damage.AppliedDamage>0 && target is ICombatImpactTarget feedback) feedback.ReceiveImpact(contact.Value);
                if(damage.AppliedDamage>0 && !damage.Killed && target is IReactionTarget reaction) reaction.ReceiveReaction(_values.Impact,_direction);
            }
            Retire(ProjectileEnd.Impact,contact); return;
        }
        GlobalPosition=to; _age+=dt; _distance+=travel;
        if(_age>=_values.Lifetime || _distance>=_values.Distance) Retire(ProjectileEnd.Expired);
    }
    // Point hit geometry is separate from decorative shaft/height; arrows never block actor movement.
    // =========================================================
    public override void _Draw()
    {
        Vector2 tip=new Vector2(0,-_values.Height).Rotated(-GlobalRotation),tail=tip+new Vector2(-_values.Length,0);
        DrawLine(tail,tip,_values.Colour,1); DrawLine(tip,tip+new Vector2(-3,-2),_values.Colour,1); DrawLine(tip,tip+new Vector2(-3,2),_values.Colour,1);
    }
    // Lifecycle event is the seam for future impact presentation; normal retirement cannot keep ticking.
    // =========================================================
    public void Retire(ProjectileEnd reason=ProjectileEnd.Cancelled,AttackContact? contact=null)
    {
        if(_released) return;
        _released=true; _active=false; SetPhysicsProcess(false); Hide(); _spawner.Release();
        QueueFree(); var finished=Finished; Finished=null; finished?.Invoke(reason,contact);
    }
    // Inherited process suspension cancels shots rather than resuming a stale arrow later.
    // =========================================================
    public override void _Notification(int what) { if(what==NotificationDisabled && _active) Retire(); }
    // Scene removal disposes native query data and releases the slot without post-despawn callbacks.
    // =========================================================
    public override void _ExitTree()
    {
        if(!_released) { _released=true; _spawner.Release(); }
        _active=false; SetPhysicsProcess(false); Hide(); Finished=null; _ray?.Dispose(); _ray=null; _source=null;
    }
}
