// Transient shared reaction state. Existing actor motors consume displacement; this helper never moves a body.
using Godot;
public interface IReactionTarget
{
    void ReceiveReaction(AttackValues attack, Vector2 direction);
}
public sealed class Knockback
{
    private Vector2 _direction;
    private float _pushRemaining, _pushDuration, _distance, _stagger, _burst, _grace;
    public bool Active => _pushRemaining>0 || _stagger>0;
    public bool NeedsUpdate => Active || _grace>0;
    public bool Staggered => _stagger>0;
    // Invalid authored resistance fails closed; independent axes support heavy creatures and stagger immunity.
    // =========================================================
    public bool Apply(AttackValues attack, Vector2 direction, bool immune, float pushResistance, float staggerResistance)
    {
        if (immune || _grace>0 || !direction.IsFinite() || direction.LengthSquared()<0.001f) return false;
        float push=attack.KnockbackDistance*(1-SafeResistance(pushResistance));
        float stagger=attack.StaggerSeconds*(1-SafeResistance(staggerResistance));
        if (push<=0 && stagger<=0) return false;
        _direction=direction.Normalized(); _distance=push; _pushDuration=attack.KnockbackSeconds;
        _pushRemaining=push>0 ? _pushDuration : 0;
        _stagger=Mathf.Min(Mathf.Max(_stagger,stagger),Mathf.Max(0,0.6f-_burst));
        return true;
    }
    // Integrate linear deceleration exactly over the step, including its final partial frame; no accumulated forces.
    // =========================================================
    public Vector2 Advance(float dt)
    {
        if (!float.IsFinite(dt) || dt<=0) return Vector2.Zero;
        _grace=Mathf.Max(0,_grace-dt);
        float before=_pushRemaining, after=Mathf.Max(0,before-dt);
        Vector2 velocity=_pushDuration>0 ? _direction*(_distance*(before*before-after*after)/(_pushDuration*_pushDuration*dt)) : Vector2.Zero;
        _pushRemaining=after;
        if (Active) _burst+=dt;
        _stagger=Mathf.Max(0,_stagger-dt);
        if (_burst>=0.6f) { _stagger=0; _pushRemaining=0; }
        if (!Active && _burst>0) { _burst=0; _grace=0.25f; }
        return velocity;
    }
    // Live master switches can suppress either axis of an existing reaction independently.
    // =========================================================
    public void Suppress(bool push,bool stagger) { if(push) _pushRemaining=0; if(stagger) _stagger=0; }
    // Death, suspension and unload discard all transient motion, including grace.
    // =========================================================
    public void Clear() { _direction=Vector2.Zero; _pushRemaining=_pushDuration=_distance=_stagger=_burst=_grace=0; }
    // =========================================================
    private static float SafeResistance(float value) => float.IsFinite(value) ? Mathf.Clamp(value,0,1) : 1;
}
