// C3.3c+d actual physics-contact, provenance, material, lifetime and cleanup regressions.
using Godot;
using System;
using System.Threading.Tasks;
public partial class ValidateHitFeedback : Node2D
{
    private int _checks;
    // Exercise confirmed contacts, never synthetic health events or repeated hit queries for presentation.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var bone=GD.Load<HitFeedbackProfile>("res://FEEDBACK/HitFeedback/Profiles/Bone.tres");
            var flesh=GD.Load<HitFeedbackProfile>("res://FEEDBACK/HitFeedback/Profiles/Flesh.tres");
            Check(bone.Particles.Shape==ParticleShape.Shard && flesh.Particles.Shape==ParticleShape.Droplet,"Material shapes");
            Check(bone.Colour!=flesh.Colour && bone.Particles.LifetimeMax<=0.3f,"Distinct short material profiles");
            var player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>();
            player.Config=(PlayerMasterConfig)GD.Load<PlayerMasterConfig>("res://ENTITIES/Player/DefaultPlayerConfig.tres").Duplicate();
            AddChild(player); player.SetPhysicsProcess(false);
            var target=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
            target.Configure(new EntityDefinition { MaxHealth=100,ImpactFeedback=bone,ReactionImmune=true,DeathFadeSeconds=3 });
            target.Position=new Vector2(35,0); AddChild(target); await Frames(3);
            Check(target.ImpactVisual==null && player.ImpactVisual==null,"Never-hit actors allocated renderers");
            using var profile=new AttackProfile { BaseDamage=5,HitReach=60,ContactProgress=0.1f };
            var values=profile.Snapshot(1);
            using var query=new MeleeHitQuery(player);
            Check(query.Strike(player,values,Vector2.Right,player.Stats.Values,out var hit),"Confirmed actor contact");
            Check(hit.Source==player && hit.Target==target && hit.Direction==Vector2.Right,"Source/direction provenance");
            Check(hit.Position.DistanceTo(new Vector2(23,0))<0.01f,"Closest hurt point");
            var renderer=target.ImpactVisual;
            Check(renderer!=null && renderer.EmittedBursts==1 && renderer.ActiveParticleCount==8,"Bone burst");
            Check(renderer.LastImpactPoint==hit.Position && target.Health==95,"Impact origin/damage");
            Check(query.Strike(player,values with { Damage=0 },Vector2.Right,player.Stats.Values,out _),"Zero contact");
            Check(renderer.EmittedBursts==1,"Zero damage emitted");
            Check(!query.Strike(player,values,Vector2.Left,player.Stats.Values,out _),"Rear miss");
            Check(renderer.EmittedBursts==1,"Miss emitted");
            target.TakeDamage(1); target.RestoreHealth(100);
            Check(renderer.EmittedBursts==1,"Generic damage/restoration inferred particles");
            target.Stats.SetModifier("test.immune",new(CombatStat.Defense,Flat:100));
            query.Strike(player,values,Vector2.Right,player.Stats.Values,out _);
            Check(renderer.EmittedBursts==1,"Armour-absorbed hit emitted"); target.Stats.RemoveModifier("test.immune");
            target.Config.ImpactFeedback=null; query.Strike(player,values,Vector2.Right,player.Stats.Values,out _);
            Check(renderer.EmittedBursts==1,"Absent profile emitted"); target.Config.ImpactFeedback=bone;
            int children=target.GetChildCount();
            for(int i=0;i<20;i++) query.Strike(player,values with { Damage=1 },Vector2.Right,player.Stats.Values,out _);
            Check(renderer.ActiveParticleCount==32 && target.GetChildCount()==children,"Overflow/node reuse");
            target.Position+=new Vector2(10,20); target.Scale=new Vector2(1.2f,0.8f);
            Check(renderer.LastImpactPoint==hit.Position,"Recoil changed snapshotted impact");
            await Frames(30); Check(!renderer.IsProcessing() && renderer.ActiveParticleCount==0,"Settled renderer did not sleep");
            target.Position=new Vector2(35,0); target.Scale=Vector2.One; await Frames(3);
            query.Strike(player,values,Vector2.Right,player.Stats.Values,out _);
            target.ProcessMode=ProcessModeEnum.Disabled;
            Check(renderer.ActiveParticleCount==0 && !renderer.IsProcessing(),"Suspension cleanup");
            target.ProcessMode=ProcessModeEnum.Inherit; await Frames(3);
            Check(renderer.ActiveParticleCount==0,"Reactivation replayed impact");
            using var enemyQuery=new MeleeHitQuery(target);
            player.Guard.Start(Vector2.Right); player.Guard.Profile.Reduction=1;
            Check(enemyQuery.Strike(target,values with { Targets=CombatFaction.Player },Vector2.Left,target.Stats.Values,out var blocked),"Guard contact");
            Check(blocked.Damage?.AppliedDamage==0 && player.ImpactVisual==null,"Zero block emitted flesh");
            player.Guard.Profile.Reduction=0.6f;
            enemyQuery.Strike(target,values with { Targets=CombatFaction.Player },Vector2.Left,target.Stats.Values,out var chip);
            Check(chip.Damage?.Blocked==true && chip.Damage?.AppliedDamage>0 && player.ImpactVisual?.EmittedBursts==1,"Chip damage treatment");
            player.StopGuard();
            Check(enemyQuery.Strike(target,values with { Targets=CombatFaction.Player },Vector2.Left,target.Stats.Values,out _),"Enemy strike");
            Check(player.ImpactVisual?.ActiveParticleCount==12 && player.ImpactVisual.EmittedBursts==2,"Player flesh impact");
            using(var attack=new AttackExecution(player))
            {
                int before=renderer.EmittedBursts;
                Check(attack.TryStart(profile,Vector2.Right),"Executor start");
                attack.Advance(0.1); attack.Advance(0.1);
                Check(renderer.EmittedBursts==before+1,"Multiple bursts from single swing");
            }
            int beforeKill=renderer.EmittedBursts;
            query.Strike(player,values with { Damage=999 },Vector2.Right,player.Stats.Values,out var killed);
            Check(killed.Damage?.Killed==true && renderer.ActiveParticleCount>0 && renderer.EmittedBursts==beforeKill+1,"Killing blow omitted impact");
            target.QueueFree(); await Frames(3);
            Check(!GodotObject.IsInstanceValid(renderer),"Retired actor retained renderer");
            GD.Print($"C3.3c+d PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // =========================================================
    private void Check(bool value,string message) { _checks++; if(!value) throw new Exception(message); }
}
