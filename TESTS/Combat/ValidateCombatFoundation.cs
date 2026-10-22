// C1 deterministic contracts plus real player/entity lifecycle integration; no real saves are touched.
using Godot;
using System;
using System.IO;
using System.Threading.Tasks;
public partial class ValidateCombatFoundation : Node
{
    private int _checks;
    // Exercise pure contracts before instantiating production actors.
    // =========================================================
    public override async void _Ready()
    {
        try { Contracts(); await Actors(); GD.Print($"C1 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // Validate damage boundaries, authoritative HP and cache invalidation/stacking semantics.
    // =========================================================
    private void Contracts()
    {
        var h = new ActorHealth(10); int events=0, deaths=0;
        h.Changed += c => { events++; if (c.Before>0 && c.After==0) deaths++; };
        var neutral=EffectiveStats.Neutral;
        Check(DamageResolver.Resolve(new(0.5f),neutral,neutral)==1,"Half rounding");
        Check(DamageResolver.Resolve(new(0.49f),neutral,neutral)==0,"Sub-half rounding");
        Check(DamageResolver.Resolve(new(4),neutral with { DamageMultiplier=2 },neutral with { Defense=3 })==5,"Scale before defense");
        Check(DamageResolver.Resolve(new(float.MaxValue),neutral,neutral)==1000000,"Damage overflow cap");
        var blocked=DamageResolver.Apply(h,new(1),neutral,neutral with { Defense=10 });
        Check(blocked.AppliedDamage==0 && events==0,"Blocked damage emitted change");
        var hit=DamageResolver.Apply(h,new(3),neutral,neutral);
        Check(hit.HealthBefore==10 && hit.HealthAfter==7 && hit.AppliedDamage==3 && !hit.Killed,"Damage result");
        Check(h.Heal(int.MaxValue)==3 && h.Current==10,"Heal cap");
        var lethal=DamageResolver.Apply(h,new(99),neutral,neutral);
        Check(lethal.Killed && lethal.AppliedDamage==10 && h.Current==0,"Lethal clamp");
        Check(!DamageResolver.Apply(h,new(99),neutral,neutral).Killed && deaths==1,"Duplicate death");
        Check(h.Heal(10)==0 && h.Current==0,"Implicit revive");
        h.Restore(6); Check(h.IsAlive && h.Current==6,"Explicit restore");
        Reject(() => h.Restore(11)); Reject(() => new ActorHealth(0));
        Reject(() => DamageResolver.Resolve(new(float.NaN),neutral,neutral));
        Reject(() => DamageResolver.Resolve(new(-1),neutral,neutral));
        using var profile = new CombatStatProfile();
        using var stats = new ActorStats(profile); using var other = new ActorStats(profile);
        Check(stats.Values==neutral && other.Values==neutral,"Neutral baseline");
        int builds=stats.RebuildCount;
        for (int i=0;i<10000;i++) Check(stats.Values==neutral,"Idle cache changed");
        Check(stats.RebuildCount==builds,"Idle cache rebuilt");
        stats.SetModifier("flat",new(CombatStat.DamageMultiplier,Flat:1));
        stats.SetModifier("percent",new(CombatStat.DamageMultiplier,Percent:0.5f));
        stats.SetModifier("times",new(CombatStat.DamageMultiplier,Multiplier:2));
        Check(stats.Values.DamageMultiplier==6 && other.Values.DamageMultiplier==1,"Stacking/instance isolation");
        builds=stats.RebuildCount; stats.SetModifier("times",new(CombatStat.DamageMultiplier,Multiplier:2));
        Check(stats.Values.DamageMultiplier==6 && stats.RebuildCount==builds,"No-op invalidated");
        Check(stats.RemoveModifier("flat") && stats.Values.DamageMultiplier==3,"Remove modifier");
        profile.DamageMultiplier=2;
        Check(stats.Values.DamageMultiplier==6 && other.Values.DamageMultiplier==2,"Profile change invalidation");
        using var reversed=new ActorStats(profile);
        reversed.SetModifier("times",new(CombatStat.DamageMultiplier,Multiplier:2));
        reversed.SetModifier("percent",new(CombatStat.DamageMultiplier,Percent:0.5f));
        Check(reversed.Values==stats.Values,"Insertion order changed stats");
        stats.SetModifier("stop",new(CombatStat.MovementSpeedMultiplier,Percent:-2));
        stats.SetModifier("slow",new(CombatStat.AttackSpeedMultiplier,Multiplier:0));
        Check(stats.Values.MovementSpeedMultiplier==0 && stats.Values.AttackSpeedMultiplier==0.05f,"Speed bounds");
        stats.SetModifier("zero",new(CombatStat.DamageMultiplier,Multiplier:0));
        Check(stats.Values.DamageMultiplier==0,"Zero multiplier");
        Reject(() => stats.SetModifier("bad",new(CombatStat.Defense,Multiplier:float.NaN)));
        stats.Dispose(); profile.Defense=2; Reject(() => { var unused=stats.Values; });
        // Extreme products must recover when later factors shrink them, without intermediate overflow.
        using var extreme=new ActorStats();
        for(int i=0;i<100;i++) extreme.SetModifier($"a{i:D3}",new(CombatStat.DamageMultiplier,Multiplier:10000));
        for(int i=0;i<100;i++) extreme.SetModifier($"b{i:D3}",new(CombatStat.DamageMultiplier,Multiplier:0.0001f));
        Check(Math.Abs(extreme.Values.DamageMultiplier-1)<0.001,"Intermediate product overflow");
    }
    // Production scenes share the contract and keep tool compatibility and bounded feedback work.
    // =========================================================
    private async Task Actors()
    {
        var entity=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        AddChild(entity); Check(entity.Health==3 && !entity.IsProcessing(),"Entity baseline/idle");
        entity.ReceiveHit(Vector2.Right); Check(entity.Health==2 && entity.IsProcessing(),"Legacy tool adapter");
        await ToSignal(GetTree().CreateTimer(0.25),SceneTreeTimer.SignalName.Timeout);
        Check(!entity.IsProcessing(),"Hit feedback did not sleep");
        entity.Stats.SetModifier("armor",new(CombatStat.Defense,Flat:2));
        Check(entity.ReceiveDamage(new(1),EffectiveStats.Neutral).AppliedDamage==0 && !entity.IsProcessing(),"Blocked hit flash");
        entity.RestoreHealth(3); Check(entity.Health==3 && !entity.IsProcessing(),"Restore feedback");
        var player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); AddChild(player);
        var tool=player.GetNode<EquippedTool>("EquippedAxe");
        Input.ActionPress("player_right");
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        Check(Mathf.IsEqualApprox(player.Velocity.X,player.Config.WalkSpeed),"Neutral movement changed");
        player.Stats.SetModifier("slow",new(CombatStat.MovementSpeedMultiplier,Multiplier:0.5f));
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        Check(Mathf.IsEqualApprox(player.Velocity.X,player.Config.WalkSpeed*0.5f),"Movement stat not consumed");
        Input.ActionRelease("player_right");
        var result=player.ReceiveDamage(new(500),EffectiveStats.Neutral);
        Check(result.Killed && !player.IsPhysicsProcessing() && !tool.IsProcessing() && !tool.Visible,"Player death gating");
        player.HealthState.Restore(50);
        Check(player.HealthState.Current==50 && player.IsPhysicsProcessing() && tool.IsProcessing(),"Player restore gating");
        Check(!player.IsInGroup("world_tool_targets"),"Player became an axe target");
        player.QueueFree(); entity.QueueFree(); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
    // Assertions report the first violated contract with a failing process exit code.
    // =========================================================
    private void Check(bool value,string message) { _checks++; if(!value) throw new InvalidOperationException(message); }
    // Invalid inputs must fail at their boundary instead of poisoning cached state.
    // =========================================================
    private void Reject(Action action)
    { bool rejected=false; try { action(); } catch(Exception e) when(e is ArgumentException or InvalidDataException or ObjectDisposedException) { rejected=true; } Check(rejected,"Invalid input accepted"); }
}
