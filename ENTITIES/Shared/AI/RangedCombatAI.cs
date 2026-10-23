// Plain ranged intent state. GroundChase alone ticks/moves the actor; LOS and side searches are bounded.
using Godot;
using System;
public sealed class RangedCombatAI : IDisposable
{
    private readonly Entity _actor;
    private readonly CharacterBody2D _target;
    private readonly ProjectileSpawner _spawner;
    private readonly RangedCombatValues _values;
    private readonly PhysicsRayQueryParameters2D _ray;
    private readonly CollisionShape2D _targetFeet;
    private readonly EntityRangedWeapon _weapon;
    private float _drawDuration,_lockDuration,_shotCooldown;
    private float _cooldown,_elapsed,_losTimer,_positionTimer,_lockedDistance;
    private bool _drawing,_holdingRange,_clear,_locked,_hasLOS;
    private Vector2 _aim=Vector2.Right,_destination;
    public bool Drawing => _drawing;
    public bool AimLocked => _locked;
    public Vector2 Aim => _aim;
    public Vector2 Destination => _destination;
    public int ShotsFired { get; private set; }
    public int LOSQueries { get; private set; }
    public int PositionSearches { get; private set; }
    // All native/visual data is lazy: only an opted-in actor with valid injected services reaches this constructor.
    // =========================================================
    public RangedCombatAI(Entity actor,CharacterBody2D target,ProjectileSpawner spawner,RangedCombatProfile profile)
    {
        if(actor.GetWorld2D()!=spawner.GetWorld2D()) throw new ArgumentException("Ranged services must share the actor world.");
        _actor=actor; _target=target; _spawner=spawner; _values=profile.Snapshot(actor.Stats.Values);
        _targetFeet=target.GetNodeOrNull<CollisionShape2D>("Collision");
        _ray=new PhysicsRayQueryParameters2D { CollisionMask=3,CollideWithAreas=false,CollideWithBodies=true,HitFromInside=true,
            Exclude=new Godot.Collections.Array<Rid> { actor.GetRid() } };
        _losTimer=(actor.GetInstanceId()%200)/1000f; _destination=target.GlobalPosition;
        if(profile.Weapon?.Sprite!=null)
        { _weapon=new EntityRangedWeapon { Name="RangedWeapon" }; _weapon.Configure(profile.Weapon); actor.AddChild(_weapon); _weapon.Pose(_aim,0); }
    }
    // Return true to hold locomotion. A released arrow keeps its locked direction even if the target dodges.
    // =========================================================
    public bool Advance(float delta,StaticObstacleNavigation navigation,bool enabled)
    {
        if(!enabled || !GodotObject.IsInstanceValid(_spawner) || !_spawner.IsInsideTree() || !_spawner.CanProcess()
            || _spawner.IsQueuedForDeletion() || _target is not ICombatTarget target || !CombatTargeting.Eligible(_actor,target,_values.Targets))
        { Cancel(); return true; }
        _cooldown=Mathf.Max(0,_cooldown-delta); _positionTimer=Mathf.Max(0,_positionTimer-delta); _losTimer-=delta;
        Vector2 targetPoint=GodotObject.IsInstanceValid(_targetFeet) ? _targetFeet.GlobalPosition : _target.GlobalPosition;
        Vector2 offset=targetPoint-_actor.FeetCenter; float distance=offset.Length();
        if(!_locked && distance>0.01f) _aim=offset/distance;
        if(distance>_values.Maximum) { _holdingRange=false; CancelDraw(); _weapon?.Pose(_aim,0); _destination=_target.GlobalPosition; return false; }
        if(distance<_values.Minimum) { CancelDraw(); _weapon?.Pose(_aim,0); return true; }
        if(distance<=_values.Preferred) _holdingRange=true;
        if(_losTimer<=0 && !_drawing) { _losTimer=0.2f; _clear=ClearLine(_actor.FeetCenter,targetPoint); _hasLOS=true; }
        if(!_hasLOS) return true;
        if(_drawing)
        {
            _elapsed+=delta;
            if(!_locked && _elapsed>=_drawDuration-_lockDuration) { _locked=true; _lockedDistance=distance; }
            _weapon?.Pose(_aim,Mathf.Clamp(_elapsed/_drawDuration,0,1));
            if(_elapsed<_drawDuration) return true;
            Vector2 muzzle=_actor.FeetCenter+_values.Muzzle.Rotated(_aim.Angle());
            bool clear=ClearLine(_actor.FeetCenter,_actor.FeetCenter+_aim*Mathf.Max(_lockedDistance,_values.Muzzle.Length()));
            if(clear && _spawner.TryFire(_actor,_values.Projectile,muzzle,_aim,out _)) ShotsFired++;
            _cooldown=_shotCooldown; CancelDraw(); _weapon?.Pose(_aim,0); return true;
        }
        _weapon?.Pose(_aim,0);
        if(_holdingRange && _clear)
        {
            if(_cooldown<=0)
            {
                float speed=_actor.Stats.Values.AttackSpeedMultiplier;
                _drawDuration=_values.Windup/speed; _lockDuration=Mathf.Min(_values.LockSeconds,_drawDuration);
                _shotCooldown=_values.Cooldown/speed; _drawing=true; _elapsed=0; _locked=false;
            }
            return true;
        }
        if(!_clear && navigation!=null && _positionTimer<=0)
        {
            _positionTimer=1; _destination=_target.GlobalPosition; PositionSearches++;
            float angle=(_actor.GlobalPosition-_target.GlobalPosition).Angle();
            foreach(float turn in Turns)
            {
                Vector2 candidate=_target.GlobalPosition+Vector2.FromAngle(angle+turn)*_values.Preferred;
                Vector2 feet=candidate+(_actor.FeetCenter-_actor.GlobalPosition);
                if(!navigation.IsWalkable(candidate) || !_spawner.Allows(feet) || !ClearLine(feet,targetPoint)) continue;
                if(navigation.FindPath(_actor.GlobalPosition,candidate).Length==0) continue;
                _destination=candidate; break;
            }
        }
        else if(_clear) _destination=_target.GlobalPosition;
        return false;
    }
    private static readonly float[] Turns={ -Mathf.Pi/4,Mathf.Pi/4,-Mathf.Pi/2,Mathf.Pi/2 };
    // Inclusive body ray: allies block firing lines rather than receiving friendly damage or causing endless shooting.
    // =========================================================
    private bool ClearLine(Vector2 from,Vector2 to)
    {
        if(!_spawner.AllowsSegment(from,to)) return false;
        if(!_values.RequireLOS) return true;
        _ray.From=from; _ray.To=to;
        var hit=_actor.GetWorld2D().DirectSpaceState.IntersectRay(_ray); LOSQueries++;
        return hit.Count==0 || hit["collider"].AsGodotObject()==_target;
    }
    // Non-interrupting recoil pauses a draw; it never releases a projectile from the reaction branch.
    // =========================================================
    public void Pause(float delta) { _cooldown=Mathf.Max(0,_cooldown-delta); }
    // Explicit C3.2 interruption consumes the pending shot and preserves a cooldown.
    // =========================================================
    public void Interrupt() { if(_drawing) _cooldown=Mathf.Max(_cooldown,_shotCooldown); CancelDraw(); _weapon?.Pose(_aim,0); }
    // =========================================================
    private void CancelDraw() { _drawing=false; _locked=false; _elapsed=0; }
    // Target loss, disable and shutdown leave no pending shot or cached firing line.
    // =========================================================
    public void Cancel(bool hide=false) { CancelDraw(); _clear=false; _holdingRange=false; _losTimer=0; _positionTimer=0; _hasLOS=false; if(hide) _weapon?.Hide(); else _weapon?.Pose(_aim,0); }
    // =========================================================
    public void Dispose() { _ray.Dispose(); if(GodotObject.IsInstanceValid(_weapon) && !_weapon.IsQueuedForDeletion()) { _weapon.Hide(); _weapon.Name="RetiredWeapon"+_weapon.GetInstanceId(); _weapon.QueueFree(); } }
}
