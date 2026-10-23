// Lazy actor-owned impact canvas; fixed world coordinates keep existing particles independent of recoil/hops.
using Godot;
public partial class HitFeedback : Node2D
{
    private readonly ParticleEmitter _particles=new(32);
    private readonly RandomNumberGenerator _random=new();
    public int ActiveParticleCount => _particles.ActiveCount;
    public int EmittedBursts { get; private set; }
    public Vector2 LastImpactPoint { get; private set; }

    // One canvas per struck actor, never one Node per hit/particle; actors with no impacts allocate none.
    // =========================================================
    public static HitFeedback Emit(Node2D owner,HitFeedback current,HitFeedbackProfile profile,AttackContact contact)
    {
        if (!GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres").ImpactFeedbackEnabled || profile?.Enabled!=true || profile.Particles==null || profile.Count<=0 || contact.Damage?.AppliedDamage is not >0
            || contact.Target!=owner || !contact.Position.IsFinite() || !contact.Direction.IsFinite()
            || !GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree() || owner.IsQueuedForDeletion() || !owner.CanProcess()) return current;
        if (!GodotObject.IsInstanceValid(current))
        {
            current=new HitFeedback { Name="HitFeedback" };
            owner.AddChild(current);
        }
        current.Spawn(profile,contact); return current;
    }
    // Dormant until an authoritative damaging contact wakes it. No health-event subscription or hit query.
    // =========================================================
    public override void _Ready() { _random.Seed=GetInstanceId(); SetProcess(false); }
    // Snapshot each burst; profile edits cannot rewrite existing slots or move their impact origin.
    // =========================================================
    private void Spawn(HitFeedbackProfile profile,AttackContact contact)
    {
        Vector2 offset=profile.VisualOffset.IsFinite() ? profile.VisualOffset.Clamp(new Vector2(-64,-64),new Vector2(64,64)) : Vector2.Zero;
        LastImpactPoint=contact.Position;
        _particles.EmitBurst(profile.Particles.Snapshot(),Mathf.Clamp(profile.Count,0,12),new Rect2(contact.Position+offset,Vector2.Zero),
            Transform2D.Identity,contact.Direction.Normalized(),profile.Colour,_random);
        if (!_particles.Active) return;
        EmittedBursts++; QueueRedraw(); SetProcess(true);
    }
    // Only active slots run; expiry schedules the final clearing redraw and sleeps immediately.
    // =========================================================
    public override void _Process(double delta)
    {
        _particles.Advance((float)delta,Vector2.Zero); QueueRedraw();
        if (!_particles.Active) SetProcess(false);
    }
    // Keep actor Y-sort/canvas ownership; inverse draw transforms prevent particles following recoil/scale.
    // =========================================================
    public override void _Draw() => _particles.Draw(this,worldSpace:true);
    // Suspension/unload must never resume stale impacts; ordinary death allows its short final burst.
    // =========================================================
    public override void _Notification(int what) { if (what==NotificationDisabled) Clear(); }
    // =========================================================
    private void Clear() { bool active=_particles.Active; _particles.Clear(); if (active) QueueRedraw(); SetProcess(false); }
    // =========================================================
    public override void _ExitTree() { Clear(); _random.Dispose(); }
}
