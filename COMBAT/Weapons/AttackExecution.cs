// One shared request/timing/contact/recovery/cooldown state machine for player tools and enemy decisions.
using Godot;
using System;
public enum AttackPhase { Idle, Windup, Contact, Recovery, Cooldown }
public sealed class AttackExecution : IDisposable
{
    private readonly ICombatTarget _source;
    private readonly MeleeHitQuery _query;
    private readonly AttackContact[] _contacts = new AttackContact[MeleeHitQuery.MaxTargetCount];
    private AttackValues _values;
    private ToolWork _toolWork;
    private EffectiveStats _stats;
    private float _elapsed;
    private bool _contacted, _disposed, _interrupted;
    public bool Busy { get; private set; }
    public bool IsSwinging => Busy && !_interrupted && _elapsed<_values.Duration;
    public float Progress => Busy && !_interrupted ? Mathf.Clamp(_elapsed/_values.Duration,0,1) : 1;
    public Vector2 Aim { get; private set; }
    public int QueryCount => _query.QueryCount;
    public int OverflowCount => _query.OverflowCount;
    public AttackPhase Phase => !Busy ? AttackPhase.Idle : !IsSwinging ? AttackPhase.Cooldown
        : Progress<_values.ContactStart ? AttackPhase.Windup : Progress<=_values.ContactEnd ? AttackPhase.Contact : AttackPhase.Recovery;
    public event Action<AttackContact> Contact;

    // Query lifetime belongs to the actor adapter; this state has no Node callbacks or timers.
    // =========================================================
    public AttackExecution(ICombatTarget source)
    {
        if (source?.CombatNode is not CollisionObject2D body) throw new ArgumentException("Melee source must have a physics body.");
        _source=source; _query=new MeleeHitQuery(body);
    }
    // Alive, idle requests capture aim, timing and attacker stats exactly once.
    // =========================================================
    public bool TryStart(AttackProfile profile, Vector2 aim, int targetBonus = 0, ToolWork toolWork = default)
    {
        if (_disposed || Busy || profile==null || !CombatTargeting.Active(_source) || !aim.IsFinite() || aim.LengthSquared()<0.0001f) return false;
        toolWork.Validate(); _toolWork=toolWork;
        _stats=_source.Stats.Values; _values=profile.Snapshot(_stats.AttackSpeedMultiplier,targetBonus);
        Aim=aim.Normalized(); _elapsed=0; _contacted=false; _interrupted=false; Busy=true; return true;
    }
    // Advance only while active, on the owner's physics path. Crossing a tiny window still gets one contact sample.
    // =========================================================
    public void Advance(double delta)
    {
        if (!Busy) return;
        if (!CombatTargeting.Active(_source)) { Cancel(); return; }
        if (!double.IsFinite(delta) || delta<0) throw new ArgumentOutOfRangeException(nameof(delta));
        float before=_elapsed;
        _elapsed=(float)Math.Min(_values.Duration+_values.Cooldown,_elapsed+delta);
        if (!_contacted && before<=_values.ContactEnd*_values.Duration && _elapsed>=_values.ContactStart*_values.Duration)
        {
            int hits=_query.StrikeMany(_source,_values,Aim,_stats,_contacts,_toolWork);
            if (hits>0)
            {
                _contacted=true;
                for(int i=0;i<hits;i++) Contact?.Invoke(_contacts[i]);
            }
        }
        if (_elapsed>=_values.Duration+_values.Cooldown) Busy=false;
    }
    // Interrupt windup/contact/recovery without refunding the remaining attack or cooldown.
    // =========================================================
    public void Interrupt() { if (Busy) { _contacted=true; _interrupted=true; } }
    // Death, disabled owners, equipment shutdown and scene exit cancel pending contact immediately.
    // =========================================================
    public void Cancel() { Busy=false; _contacted=false; _elapsed=0; }
    // Release native query objects after the adapter unsubscribes from intent/health events.
    // =========================================================
    public void Dispose() { if (_disposed) return; _disposed=true; Cancel(); Contact=null; _query.Dispose(); }
}
