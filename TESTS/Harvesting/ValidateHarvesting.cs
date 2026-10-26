// H0-H3 invariants and native vertical slice. All storage/drop/tree state is disposable; no real campaign IO.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using GameTree = FantasyGame.World.Trees.Tree;
using FantasyGame.WorldInteractions.Fire;

public partial class ValidateHarvesting : Node2D
{
    private int _checks;
    private HarvestingPractice _room;
    // Test authoring, deterministic/atomic rewards, pickup transactions and real confirmed axe chops.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            string fingerprint=CampaignSession.ContentFingerprint();
            _room=GD.Load<PackedScene>("res://TESTS/Harvesting/HarvestingPractice.tscn").Instantiate<HarvestingPractice>(); AddChild(_room);
            _room.Player.SetPhysicsProcess(false); await Frames(4);
            await RewardsAndWork(); await Drops(); await Trees();
            Check(CampaignSession.ContentFingerprint()==fingerprint,"Fixture does not mutate production content fingerprint");
            var guardedWorld=new WorldScene(); var guardedTree=new GameTree(); guardedWorld.AddChild(guardedTree);
            Rejects(()=>guardedTree.EnableHarvesting(_room.Drops,"forbidden"),"WorldScene production gate rejects harvesting","H4"); guardedWorld.Free();
            Check(GD.Load<TreeDefinition>("res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres").Harvesting!=null,"Production Oak has shared harvesting profile");
            Check(GD.Load<EquippedItemDefinition>("res://ITEMS/Tools/Axes/StarterAxe.tres").PrimaryHarvestWork==1,"Production axe supplies chopping work");
            _room.QueueFree(); await Frames(4);
            GD.Print($"HARVEST H0-H3 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Generic extraction proves source-independent logic, snapshots, wrong-tool rejection and once-only completion.
    // =========================================================
    private async Task RewardsAndWork()
    {
        var catalog=_room.Drops.Catalog; var log=catalog.Get("wooden_log");
        var entry=new DropEntry { Item=log,Minimum=3,Maximum=5 };
        var table=new DropTable { Entries=new() { entry } }; var plan=table.Compile(catalog);
        int first=plan.Roll("scope/source/0")[0].Quantity; entry.Minimum=entry.Maximum=99;
        Check(plan.Roll("scope/source/0")[0].Quantity==first && first>=3 && first<=5,"Compiled reward snapshot stable");
        bool varied=false; for(int i=1;i<24;i++) if(plan.Roll("scope/source/"+i)[0].Quantity!=first) varied=true;
        Check(varied,"Independent source IDs vary rewards");
        var noChance=new DropTable { Entries=new() { new DropEntry { Item=log,Chance=0 } } };
        Check(noChance.Compile(catalog).Roll("scope/zero")[0].Quantity==0,"Zero chance never awards");
        var invalid=new DropTable { Entries=new() { new DropEntry { Item=new ItemDefinition { Id=log.Id } } } };
        Rejects(()=>invalid.Compile(catalog),"Noncanonical item rejected");
        Rejects(()=>new DropScatterProfile { HopHeight=float.NaN }.Snapshot(),"Invalid scatter rejected");
        Rejects(()=>new DropScatterProfile { LandingBounceSeconds=float.NaN }.Snapshot(),"Invalid landing bounce rejected");
        Rejects(()=>new DropScatterProfile { LandingBounceCount=4 }.Snapshot(),"Excessive landing bounces rejected");
        entry.Minimum=3; entry.Maximum=5;
        var profile=new HarvestProfile { RequiredWork=4,Rewards=table };
        var harvest=new Harvestable(); harvest.Initialize(profile,catalog,"test/generic/0"); AddChild(harvest);
        int completes=0; bool admit=false; int chosen=0;
        harvest.TryComplete=(rewards,hit)=> { completes++; if(chosen==0) chosen=rewards[0].Quantity; Check(rewards[0].Quantity==chosen,"Retry cannot reroll rewards"); return admit; };
        var valid=new HarvestHit(_room.Player,new ToolWork("axe",1),Vector2.Right,Vector2.Zero);
        Check(harvest.Apply(valid with { Tool=new("sword",1) })==HarvestWorkResult.Rejected && harvest.Progress==0,"Wrong tool adds no work");
        Check(harvest.Apply(valid with { Tool=new("axe",float.NaN) })==HarvestWorkResult.Rejected,"Invalid work rejected");
        for(int i=1;i<=3;i++) Check(harvest.Apply(valid)==HarvestWorkResult.Progress && harvest.Progress==i,"Accepted generic work "+i);
        Check(harvest.Apply(valid)==HarvestWorkResult.Rejected && harvest.Progress==3 && !harvest.Completed,"Failed reward reservation leaves source harvestable");
        admit=true; Check(harvest.Apply(valid)==HarvestWorkResult.Completed && completes==2,"Retry commits one logical completion");
        Check(harvest.Apply(valid)==HarvestWorkResult.Rejected && completes==2,"Completed source cannot generate another batch");
        Check(!harvest.IsProcessing() && !harvest.IsPhysicsProcessing(),"Idle harvest component has no callbacks");
        harvest.QueueFree(); await Frames(2);
    }
    // Reproducible hop/scatter, bounded journals, full pack failures, spatial collection and duplicate/reentrant safety.
    // =========================================================
    private async Task Drops()
    {
        var drops=_room.Drops; var log=drops.Catalog.Get("wooden_log"); var player=_room.Player;
        string eventId=drops.ScopeId+"/h1/0"; Vector2 origin=new(430,310);
        Check(drops.TryReserve(eventId,origin,new[] { new DropReward(log,6) }),"Manual H1 reward reservation");
        var record=drops.Capture().Single(); Check(record.Quantity==6 && !record.Released,"Large quantity coalesces to one reserved stack");
        Check(!drops.TryReserve(eventId,origin,new[] { new DropReward(log,6) }) && drops.RecordCount==1,"Repeated event cannot spawn duplicates");
        Check(origin.DistanceTo(record.Landing)<=58 && record.Landing!=origin,"Bounded scatter");
        drops.Release(eventId); var node=drops.FindNode(record.Id); drops.Release(eventId);
        Check(drops.FindNode(record.Id)==node && !node.Settled,"Release idempotent and airborne");
        node._Process(0.275); var art=node.GetNode<Sprite2D>("Artwork");
        Check(art.Position.Y<-20 && node.GlobalPosition.DistanceTo(origin)<origin.DistanceTo(record.Landing),"Artwork hops above moving ground root");
        player.GlobalPosition=record.Landing; Check(!drops.TryCollectNearest(player,_room.Storage),"Airborne item not collected");
        node._Process(0.275); Check(!node.Settled && node.IsProcessing(),"Touchdown begins a short rebound instead of settling immediately");
        node._Process(0.07); Check(art.Position.Y<-8 && node.GlobalPosition.IsEqualApprox(record.Landing),"Artwork rebounds while grounded root stays at its landing");
        Check(!drops.TryCollectNearest(player,_room.Storage),"Bouncing item cannot be collected prematurely");
        node._Process(2); Check(node.Settled && !node.IsProcessing() && !node.IsPhysicsProcessing(),"Rebounds finish and pickup is dormant");
        Check(Mathf.IsEqualApprox(art.Position.Y,-7),"Bounce returns artwork to its original grounded offset");
        player.GlobalPosition=record.Landing+Vector2.Right*40;
        var wall=Block(record.Landing+Vector2.Right*20,new Vector2(8,40)); await Frames(3);
        Check(!drops.TryCollectNearest(player,_room.Storage),"Cannot collect through solid obstruction");
        wall.QueueFree(); await Frames(3); player.GlobalPosition=record.Landing;
        var full=new InventoryStorage(drops.Catalog,1); full.TryAdd(drops.Catalog.Get("starteraxe"),1);
        Check(!drops.TryCollectNearest(player,full) && !drops.Capture()[0].Collected && drops.LiveCount==1,"Full inventory leaves drop unchanged");
        var wrong=new ItemCatalog { Items=new() { log } }; wrong.Initialize();
        Check(!drops.TryCollectNearest(player,new InventoryStorage(wrong,2)),"Collector catalog mismatch rejected");
        player.GlobalPosition=origin+new Vector2(200,0); Check(!drops.TryCollectNearest(player,_room.Storage),"Out-of-range collection rejected");
        player.GlobalPosition=record.Landing; player.HealthState.Restore(0);
        Check(!drops.TryCollectNearest(player,_room.Storage),"Dead player cannot collect"); player.HealthState.Restore(player.HealthState.Maximum); player.SetPhysicsProcess(false);
        bool reentered=false; Action reentry=()=>reentered=drops.TryCollectNearest(player,_room.Storage); _room.Storage.Changed+=reentry;
        Check(drops.TryCollectNearest(player,_room.Storage),"Collection transaction succeeds"); _room.Storage.Changed-=reentry;
        Check(!reentered && drops.Capture()[0].Collected && drops.LiveCount==0,"Reentrant listener cannot duplicate grant");
        Check(!_room.Drops.TryCollectNearest(player,_room.Storage),"Repeated pickup request cannot grant twice");
        Check(Count(log)==6,"Real inventory owns exactly the awarded quantity"); await Frames(2);
        Check(!GodotObject.IsInstanceValid(node),"Collected presentation released");
        var second=new WorldDropSpawner(); AddChild(second); second.Initialize(drops.Catalog,drops.ScopeId,new Rect2(-700,-450,1400,1000),new DropScatterProfile());
        Check(second.TryReserve(eventId,origin,new[] { new DropReward(log,6) }),"Equivalent independent scope reservation");
        var again=second.Capture().Single(); Check(again.Landing==record.Landing && again.LandingRotation==record.LandingRotation,"Scatter reproducible independent of runtime Node IDs");
        second.QueueFree(); await Frames(2);
        var capped=new WorldDropSpawner(); AddChild(capped); capped.Initialize(drops.Catalog,"cap",new Rect2(300,200,300,300),new DropScatterProfile { MaximumLivePickups=1,MaximumSessionRecords=1,MaximumBurstPickups=1 });
        capped.LandingAllowed=_=>false; Check(!capped.TryReserve("cap/blocked",origin,new[] { new DropReward(log,6) }) && capped.RecordCount==0,"Blocked landings fail atomically");
        capped.LandingAllowed=_=>true;
        var blocked=Block(origin,new Vector2(200,200)); await Frames(3);
        Check(!capped.TryReserve("cap/blocked",origin,new[] { new DropReward(log,6) }) && capped.RecordCount==0,"Solid obstructed landing paths rejected atomically");
        blocked.QueueFree(); await Frames(3);
        Check(capped.TryReserve("cap/blocked",origin,new[] { new DropReward(log,6) }),"Blocked source can retry without consumed identity");
        Check(!capped.TryReserve("cap/next",origin,new[] { new DropReward(log,6) }) && capped.RecordCount==1,"Population cap rejects entire next burst");
        Check(!capped.IsProcessing() && !capped.IsPhysicsProcessing(),"Drop coordinator never polls"); capped.QueueFree(); await Frames(2);
        // Actual E request enters via PlayerInput/PlayerPickupInteraction; inventory-open suppression remains central.
        string keyEvent=drops.ScopeId+"/h1/key"; Check(drops.TryReserve(keyEvent,origin,new[] { new DropReward(log,1) }),"Key pickup prepared"); drops.Release(keyEvent);
        var keyRecord=drops.Capture().Single(x=>x.EventId==keyEvent); drops.FindNode(keyRecord.Id)._Process(2); player.GlobalPosition=keyRecord.Landing;
        Input.UseAccumulatedInput=false; KeyPress(Key.I); KeyPress(Key.E); Check(Count(log)==6,"Open inventory suppresses collection key");
        KeyPress(Key.Escape); KeyPress(Key.E); Check(Count(log)==7,"E collects through real player adapter");
    }
    // Real equipment/attack/query contacts, fire precedence, mirrored root anchors and interrupted fall outcome.
    // =========================================================
    private async Task Trees()
    {
        var player=_room.Player; var oak=_room.Oak; var state=oak.HarvestState; var input=player.GetNode<PlayerInput>("Input");
        player.GlobalPosition=new Vector2(25,20); await Frames(3);
        var hits=oak.GetNode<TreeHitResponse>("HitResponse");
        hits.ReceiveHit(Vector2.Right); hits.ReceiveHit(Vector2.Right); Check(state.Progress==0,"Visual wobble callbacks never count as chopping");
        KeyPress(Key.Key2); input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right); await Wait(0.65);
        Check(state.Progress==0,"Actual sword attack cannot chop"); KeyPress(Key.Key1);
        input.EmitSignal(PlayerInput.SignalName.AlternateUseRequested,Vector2.Right); await Wait(0.8);
        Check(state.Progress==0,"Axe prod without configured work remains cosmetic");
        for(int i=1;i<=3;i++)
        {
            var equipment=player.GetNode<EquippedTool>("EquippedAxe").EquippedItem;
            input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right);
            if(i==1) equipment.PrimaryHarvestWork=40; // In-flight work must remain the accepted snapshot of one.
            await Wait(0.65); equipment.PrimaryHarvestWork=1;
            Check(state.Progress==i && !state.Completed,"One confirmed axe swing adds one chop "+i);
        }
        int before=_room.Drops.RecordCount;
        input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right); await Wait(0.25);
        Check(state.Completed && !GodotObject.IsInstanceValid(oak),"Fourth chop fells and releases standing tree/collider");
        var records=_room.Drops.Capture().Where(x=>x.EventId.EndsWith("oak/0/fell/0",StringComparison.Ordinal)).ToArray();
        Check(records.Length>=3 && records.Length<=5 && records.Sum(x=>x.Quantity)>=3 && records.Sum(x=>x.Quantity)<=5,"Felling reserves three to five real log units");
        Check(records.All(x=>x.Released),"Logs are released at the start of the tree fall");
        Check(records.All(x=>_room.Drops.FindNode(x.Id)?.Settled==false),"Newly released logs are animated, not already settled");
        var fall=_room.GetChildren().OfType<TreeFall>().Single(); fall._Process(0.15);
        Check(fall.GetChild<Node2D>(0).Rotation>0,"Rightward impact falls right");
        var fallArt=fall.GetChild<Node2D>(0).GetChild<Sprite2D>(0);
        Check(fallArt.Material==null && fallArt.Texture!=null,"Falling art has no standing wind clipping shader");
        fall.QueueFree(); await Frames(3);
        Check(_room.Drops.Capture().Where(x=>x.EventId==records[0].EventId).All(x=>x.Released),"Interrupted proxy leaves the already-released batch intact");
        _room.Drops.Release(records[0].EventId); Check(_room.Drops.RecordCount==before+records.Length,"Repeated fall release cannot duplicate rewards");
        var blossom=_room.Blossom; var burn=blossom.GetNode<Burnable>("Burnable");
        Check(burn.TryIgnite(blossom.GlobalPosition,"H3 test"),"Fire precedence fixture ignites");
        var hit=new HarvestHit(player,new ToolWork("axe",1),Vector2.Left,blossom.GlobalPosition);
        Check(blossom.HarvestState.Apply(hit)==HarvestWorkResult.Rejected && blossom.HarvestState.Progress==0,"Burning tree cannot award chopping work");
        burn.ResetBurn();
        var original=blossom.GetNode<Sprite2D>("Sprite"); Transform2D transform=original.GlobalTransform;
        for(int i=0;i<4;i++) blossom.GetNode<TreeHitResponse>("HitResponse").ReceiveHarvestHit(hit);
        var mirroredFall=_room.GetChildren().OfType<TreeFall>().Single(); var mirroredArt=mirroredFall.GetChild<Node2D>(0).GetChild<Sprite2D>(0);
        Check(mirroredArt.FlipH && mirroredArt.GlobalTransform.IsEqualApprox(transform),"Mirrored artwork starts at exact grounded source transform");
        mirroredFall._Process(0.2); Check(mirroredFall.GetChild<Node2D>(0).Rotation<0,"Leftward impact falls left");
        await Wait(1.5);
        Check(!_room.GetChildren().OfType<TreeFall>().Any(),"Finished fall proxies fully sleep/free");
        Check(_room.Drops.Capture().Where(x=>!x.Collected).All(x=>_room.Drops.FindNode(x.Id)?.Settled==true),"Remaining logs settle and remain collectible");
    }
    // Disposable collision fixture for landing and pickup line-of-sight checks.
    // =========================================================
    private StaticBody2D Block(Vector2 position,Vector2 size)
    { var body=new StaticBody2D { Position=position,CollisionLayer=1 }; body.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=size } }); AddChild(body); return body; }
    // Count actual canonical item units rather than UI labels.
    // =========================================================
    private int Count(ItemDefinition item) { int total=0; for(int i=0;i<_room.Storage.SlotCount;i++) if(_room.Storage.Get(i).Item==item) total+=_room.Storage.Get(i).Count; return total; }
    // Inject viewport input through the real central router.
    // =========================================================
    private void KeyPress(Key key)
    { Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=true }); Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=false }); Input.FlushBufferedEvents(); }
    // Keep engine physics and deferred teardown running during waits.
    // =========================================================
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds),SceneTreeTimer.SignalName.Timeout);
    // Allow physics/layout synchronization without busy waiting.
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // Invalid authoring must fail explicitly.
    // =========================================================
    private void Rejects(Action action,string message,string token=null) { bool rejected=false; try { action(); } catch(InvalidOperationException error) { rejected=token==null || error.Message.Contains(token,StringComparison.Ordinal); } Check(rejected,message); }
    // Fail at the exact violated contract.
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
