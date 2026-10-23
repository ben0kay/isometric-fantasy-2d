// C3.3a+b: bounded engine and actual tree adapter regressions, without changing production content.
using Godot;
using System;
using Tree=FantasyGame.World.Trees.Tree;
public partial class ValidateParticles : Node2D
{
    private int _checks;
    // Exercise numerical leaf parity and lifetime/snapshot rules before production tree integration.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            using var profile=new ParticleProfile();
            var values=profile.Snapshot();
            var emitter=new ParticleEmitter(24);
            using var random=new RandomNumberGenerator { Seed=1234 };
            using var legacy=new RandomNumberGenerator { Seed=1234 };
            var region=new Rect2(20,20,60,38);
            var transform=new Transform2D(0,new Vector2(-10,-90));
            var colour=new Color(0.9f,0.3f,0.6f,0.8f);
            emitter.EmitBurst(values,1,region,transform,Vector2.Right,colour,random);
            Vector2 oldPixel=new(legacy.RandfRange(20,80),legacy.RandfRange(20,58));
            Vector2 oldPosition=transform*oldPixel;
            Vector2 oldVelocity=new(legacy.RandfRange(-12,12)+5,legacy.RandfRange(18,29));
            float oldLife=legacy.RandfRange(2.4f,3.8f), phase=legacy.RandfRange(0,Mathf.Tau);
            Check(emitter.TryGetParticle(0,out var position,out var stored) && position.DistanceTo(oldPosition)<0.001f,"Legacy spawn coordinates");
            Check(stored==colour,"Spawn colour");
            emitter.Advance(0.1f,new Vector2(7,0));
            oldPosition+=(oldVelocity+new Vector2(7+Mathf.Sin(0.1f*5+phase)*8,0))*0.1f;
            Check(emitter.TryGetParticle(0,out position,out _) && position.DistanceTo(oldPosition)<0.001f,"Legacy leaf trajectory/wind");
            profile.LifetimeMax=0.01f; profile.Size=99;
            Check(emitter.TryGetParticle(0,out _,out stored) && stored==colour,"Resource mutation changed live colour");
            emitter.Advance(1,Vector2.Zero); Check(emitter.Active,"Resource mutation shortened live lifetime");
            emitter.EmitBurst(values,999,region,transform,Vector2.Left,Colors.White,random);
            Check(emitter.ActiveCount==24,"Capacity/overflow cap");
            emitter.Advance(10,Vector2.Zero); Check(!emitter.Active && emitter.ActiveCount==0,"Expiration");
            emitter.EmitBurst(values,2,region,transform,Vector2.Zero,colour,random);
            Check(emitter.ActiveCount==2,"Reactivation"); emitter.Clear(); Check(!emitter.Active,"Clear");
            emitter.EmitBurst(values,0,region,transform,Vector2.Zero,colour,random); Check(!emitter.Active,"Zero burst");
            emitter.EmitBurst(default,1,region,transform,Vector2.Zero,colour,random);
            Check(!emitter.Active,"Invalid/default values must not leave immortal slots");
            profile.Size=float.NaN; profile.LifetimeMin=float.PositiveInfinity;
            Check(float.IsFinite(profile.Snapshot().Size) && float.IsFinite(profile.Snapshot().LifetimeMin),"Nonfinite profile");
            foreach(string path in new[] { "res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres","res://WORLD/Content/Vegetation/Trees/PinkBlossom/PinkBlossom.tres" })
                CheckTree(GD.Load<TreeDefinition>(path));
            // The fixture's generic definition exercises no-foliage, mirrored and scaled art independently of filenames.
            var definition=new TreeDefinition { Id="test.particles",FoliageColour=colour,CanBurn=false };
            definition.VisualVariants.Add(new TreeVisualVariant { Id="test",Texture=new GradientTexture2D(),SpriteScale=new Vector2(-0.5f,0.8f) });
            CheckTree(definition);
            GD.Print($"C3.3a+b PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // Manually advance the real receiver so wobble-end notification counts are independent of frame pacing.
    // =========================================================
    private void CheckTree(TreeDefinition definition)
    {
        var tree=GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn").Instantiate<Tree>();
        tree.Configure(definition,0,true); tree.Scale=new Vector2(1.3f,0.7f); AddChild(tree);
        var hit=tree.GetNode<TreeHitResponse>("HitResponse"); var sprite=tree.GetNode<Sprite2D>("Sprite");
        Transform2D original=sprite.Transform; int notifications=0, children=tree.GetChildCount();
        hit.VisualTransformChanged+=()=>notifications++;
        Check(!hit.IsProcessing() && hit.ActiveParticleCount==0,"Never-hit tree idle");
        hit.ReceiveHit(Vector2.Right);
        Check(hit.ReceivedHits==1 && hit.ActiveParticleCount==10,"Tree burst count");
        Check(tree.GetChildCount()==children,"Per-hit child allocation");
        for(int i=0;i<8;i++) hit._Process(0.1);
        int settled=notifications;
        Check(!hit.IsWobbling && sprite.Transform==original && hit.ActiveParticleCount>0,"Wobble/leaf separation");
        hit._Process(0.1); Check(notifications==settled,"Leaf-only frame notified shadows");
        hit.SetHealthyLeaves(false); Check(hit.ActiveParticleCount==0 && !hit.IsProcessing(),"Burn clears leaves/sleeps");
        hit.ReceiveHit(Vector2.Left); Check(hit.ActiveParticleCount==0 && hit.IsWobbling,"Burnt tree wobble without leaves");
        hit.Enabled=false; Check(!hit.IsProcessing() && sprite.Transform==original,"Disable cleanup");
        hit.Enabled=true; hit.SetHealthyLeaves(true); hit.ReceiveHit(Vector2.Left);
        tree.ProcessMode=ProcessModeEnum.Disabled;
        Check(hit.ActiveParticleCount==0 && !hit.IsProcessing() && sprite.Transform==original,"Suspension cleanup");
        tree.ProcessMode=ProcessModeEnum.Inherit; hit.FoliageEnabled=false; hit.SetHealthyLeaves(true); hit.ReceiveHit(Vector2.Right);
        Check(hit.ActiveParticleCount==0,"No-foliage reset");
        hit.FoliageEnabled=true; hit.SetHealthyLeaves(true); hit.ReceiveHit(Vector2.Right);
        for(int i=0;i<50;i++) hit._Process(0.1);
        Check(!hit.IsProcessing() && hit.ActiveParticleCount==0,"Settled tree idle");
        tree.Free();
    }
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
