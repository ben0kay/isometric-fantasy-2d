// H4 real-World and separate-process disk regression. Dedicated user://h4-validation storage only.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameTree=FantasyGame.World.Trees.Tree;

public sealed record H4Expect(string PartialId,string FelledId,float HomeX,float HomeY,int Units,string WorldId);
public partial class ValidateWorldHarvesting : Node
{
    private const string Profile="dddddddddddddddddddddddddddddddd";
    private string _root;
    private int _checks;
    // Explicit write/read commands cross a real engine/process boundary; never use real campaign storage.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            _root=ProjectSettings.GlobalizePath("user://h4-validation"); CampaignSession.TestStorageRoot=_root;
            if(OS.GetCmdlineUserArgs().Contains("--h4-write")) await Write();
            else if(OS.GetCmdlineUserArgs().Contains("--h4-read")) await Read();
            else if(OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--legacy-root=",StringComparison.Ordinal)) is string legacyRoot) await ExternalLegacy(legacyRoot[14..]);
            else throw new ArgumentException("Run -- --h4-write, then -- --h4-read in a new process.");
            GD.Print($"H4 WORLD PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // A real World tree supplies partial work; a border tree commits rewards across an owner-chunk boundary.
    // =========================================================
    private async Task Write()
    {
        CampaignSession.New(Profile,123); var world=await Spawn(); var home=world.Player.GlobalPosition;
        var inventory=world.Harvesting.Inventory; var catalog=inventory.Storage.Catalog;
        Check(inventory.Storage.Capture().Length==2 && Count(world,"wooden_log")==0,"World has actual starter inventory without fake logs");
        Check(world.Player.GetNode<EquippedTool>("EquippedAxe").EquippedItem.PrimaryHarvestWork==1,"Real starter axe can chop");
        var pristine=world.Saves.Capture();
        var legacy=pristine with { SchemaVersion=2,Player=pristine.Player with { Inventory=null },ContentFingerprint=CampaignSession.LegacyHarvestFingerprint() };
        Check(HarvestSaveCompatibility.CanMigrate(),"Exact H4 introduction bridge enabled"); CampaignSession.RequireContent(legacy);
        new CampaignStore(Path.Combine(_root,"legacy")).Save(legacy,true);
        var trees=Trees(world).OrderBy(t=>t.GlobalPosition.DistanceSquaredTo(home)).ToArray(); Check(trees.Length>8,"Loaded production trees available");
        var partial=trees[0]; string partialId=Id(partial);
        var point=partial.GetNode<TreeHitResponse>("HitResponse").ClosestHitPoint(partial.GlobalPosition+Vector2.Left*200);
        world.Player.GlobalPosition=point+Vector2.Left*25; await Frames(3);
        var input=world.Player.GetNode<PlayerInput>("Input");
        for(int i=1;i<=2;i++) { input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,partial.GlobalPosition-world.Player.GlobalPosition); await Wait(0.65); Check(partial.HarvestState.Progress==i,"Real World confirmed chop "+i); }
        GameTree felled=null; string felledId=null; var originalLanding=world.Harvesting.Drops.LandingAllowed;
        foreach(var tree in trees.Where(t=>t!=partial && Edge(t.GlobalPosition,world.Config.Streaming.ChunkSize)<24).Take(30))
        {
            var owner=ChunkCoord.At(tree.GlobalPosition,world.Config.Streaming.ChunkSize);
            world.Harvesting.Drops.LandingAllowed=p=>originalLanding(p) && ChunkCoord.At(p,world.Config.Streaming.ChunkSize)!=owner;
            var hit=new HarvestHit(world.Player,new ToolWork("axe",1),Vector2.Right,tree.GlobalPosition);
            for(int i=0;i<4;i++) tree.HarvestState.Apply(hit);
            if(tree.HarvestState.Completed) { felled=tree; felledId=Id(tree); break; }
        }
        world.Harvesting.Drops.LandingAllowed=originalLanding;
        Check(felled!=null,"Border tree reserves a valid cross-chunk fall batch");
        var mid=world.Saves.Capture(); var drops=mid.Chunks.SelectMany(c=>c.Drops ?? Array.Empty<SavedDrop>()).ToArray();
        int units=drops.Sum(d=>d.Quantity); Check(units>=3 && units<=5,"Real World felling awards configured log total");
        Check(world.Harvesting.Drops.Capture().All(d=>d.Released),"Logs start immediately; mid-fall checkpoint preserves the released committed batch");
        Check(mid.Chunks.Any(c=>c.Edits.Any(e=>e.Id==felledId && e.Felled && e.Removed)),"Felled source present in same checkpoint as rewards");
        Check(mid.Chunks.Any(c=>c.Edits.Any(e=>e.Id==partialId && e.HarvestWork==2)),"Partial work recorded through existing chunk owner");
        Check(drops.All(d=>IdOwner(mid,felledId)!=ChunkCoord.At(new Vector2(d.X,d.Y),world.Config.Streaming.ChunkSize)),"Drops owned by landing chunks across source boundary");
        new CampaignStore(Path.Combine(_root,"midfall")).Save(mid,true);
        ValidationCases(mid);
        await Wait(1.5);
        var first=world.Harvesting.Drops.CaptureSaved()[0]; world.Player.GlobalPosition=new Vector2(first.X,first.Y);
        Action checkpointObserver=()=>
        {
            var during=world.Saves.Capture(); CampaignSession.Store().Validate(during);
            Check(during.Player.Inventory.Items.Where(i=>i.ItemId=="wooden_log").Sum(i=>i.Count)+during.Chunks.SelectMany(c=>c.Drops ?? Array.Empty<SavedDrop>()).Sum(d=>d.Quantity)==units,"Pickup Changed observer captures a coherent inventory/drop transaction");
        };
        inventory.Storage.Changed+=checkpointObserver;
        KeyPress(Key.E); inventory.Storage.Changed-=checkpointObserver; Check(Count(world,"wooden_log")==1,"E collects actual World drop into authoritative inventory");
        Check(world.Harvesting.Drops.CaptureSaved().Sum(d=>d.Quantity)==units-1,"Collected quantity removed from world journal");
        inventory.Hotbar.Select(1); KeyPress(Key.G); Check(world.Player.GetNode<EquippedTool>("EquippedAxe").ShieldEquipped,"G preserves temporary shield control");
        world.Player.GlobalPosition=home;
        Check(await world.Saves.SaveAsync(),"Real World checkpoint saves inventory/source/drop together");
        world.Player.GlobalPosition=new Vector2(9000,5000);
        await Until(()=>!world.Stream.IsPositionLoaded(home) && world.Stream.CanMoveTo(world.Player.GlobalPosition),"distant chunk ring");
        Check(world.Harvesting.Drops.MaterializedCount==0 && world.Harvesting.Drops.LiveCount==units-1,"Offscreen drops are logical records, not live nodes");
        Check(await world.Saves.SaveAsync(),"Offscreen logical state saved");
        world.Player.GlobalPosition=home;
        await Until(()=>world.Stream.CanMoveTo(home),"return chunk ring"); await Frames(4);
        Check(!Trees(world).Any(t=>Id(t)==felledId),"Felled tree stays gone after unload/reload");
        Check(Trees(world).Single(t=>Id(t)==partialId).HarvestState.Progress==2,"Partial chops survive unload/reload");
        Check(world.Harvesting.Drops.MaterializedCount==units-1,"Returning chunk recreates uncollected settled pickups once");
        Check(world.Harvesting.Drops.Capture().All(d=>world.Harvesting.Drops.FindNode(d.Id)?.Settled==true),"Recreated cosmetics are settled");
        Check(await world.Saves.SaveAsync(),"Return checkpoint saved");
        CheckpointFiles.Write(Path.Combine(_root,"expected.json"),new H4Expect(partialId,felledId,home.X,home.Y,units,world.Blueprint.WorldId),1024*1024);
        await Remove(world);
    }
    // A new process restores mid-fall, legacy and final checkpoints; collected items cannot respawn or be granted twice.
    // =========================================================
    private async Task Read()
    {
        var expected=CheckpointFiles.Read<H4Expect>(Path.Combine(_root,"expected.json"),1024*1024);
        CampaignSession.TestStorageRoot=Path.Combine(_root,"midfall"); CampaignSession.Continue(CampaignSession.Store().Load(Profile).Snapshot);
        var mid=await Spawn(); Check(mid.Blueprint.WorldId==expected.WorldId,"Mid-fall restore keeps WorldId");
        Check(Count(mid,"wooden_log")==0 && mid.Harvesting.Drops.LiveCount==expected.Units,"Mid-fall restore preserves exact uncollected batch");
        Check(!Trees(mid).Any(t=>Id(t)==expected.FelledId),"Mid-fall source cannot return");
        Check(mid.Harvesting.Drops.Capture().All(d=>mid.Harvesting.Drops.FindNode(d.Id)?.Settled==true),"Mid-fall save resolves to settled logical outcome"); await Remove(mid);
        CampaignSession.TestStorageRoot=Path.Combine(_root,"legacy"); var older=CampaignSession.Store().Load(Profile).Snapshot;
        Check(older.SchemaVersion==2 && older.Player.Inventory==null,"Real schema-2 disk fixture lacks inventory");
        CampaignSession.Continue(older); var legacy=await Spawn();
        Check(legacy.Harvesting.Inventory.Storage.Capture().Length==2,"Legacy campaign receives initial loadout once");
        Check(await legacy.Saves.SaveAsync(),"Legacy checkpoint upgrades to schema 3"); await Remove(legacy);
        var upgraded=CampaignSession.Store().Load(Profile).Snapshot; Check(upgraded.SchemaVersion==3 && upgraded.Player.Inventory.Items.Length==2,"Upgraded inventory stored explicitly");
        CampaignSession.Continue(upgraded); legacy=await Spawn(); Check(legacy.Harvesting.Inventory.Storage.Capture().Length==2,"Continue cannot duplicate legacy starter items"); await Remove(legacy);
        CampaignSession.TestStorageRoot=_root; var loaded=CampaignSession.Store().Load(Profile).Snapshot;
        Check(loaded.SchemaVersion==3,"World writes current inventory schema"); CampaignSession.Continue(loaded);
        var world=await Spawn(); Check(world.Blueprint.WorldId==expected.WorldId,"Continue keeps real World identity");
        Check(!Trees(world).Any(t=>Id(t)==expected.FelledId),"Continue preserves felled source");
        Check(Trees(world).Single(t=>Id(t)==expected.PartialId).HarvestState.Progress==2,"Continue preserves partial work");
        Check(Count(world,"wooden_log")==1 && world.Harvesting.Drops.LiveCount==expected.Units-1,"Inventory/remaining drops neither lost nor duplicated");
        var tool=world.Player.GetNode<EquippedTool>("EquippedAxe");
        Check(world.Harvesting.Inventory.Hotbar.SelectedSlot==1 && tool.EquippedItem.Id=="startersword" && tool.ShieldEquipped && !world.Player.Guard.Active,"Equipment/selection/shield restore without a held guard");
        var drops=world.Harvesting.Drops.CaptureSaved();
        foreach(var drop in drops) { world.Player.GlobalPosition=new Vector2(drop.X,drop.Y); KeyPress(Key.E); }
        Check(Count(world,"wooden_log")==expected.Units && world.Harvesting.Drops.LiveCount==0 && world.Harvesting.Drops.RecordCount==0,"Collecting all logs prunes drop journal without losing source tombstone");
        world.Player.GlobalPosition=new Vector2(expected.HomeX,expected.HomeY); Check(await world.Saves.SaveAsync(),"Fully collected outcome saved"); await Remove(world);
        CampaignSession.Continue(CampaignSession.Store().Load(Profile).Snapshot); world=await Spawn();
        Check(Count(world,"wooden_log")==expected.Units && world.Harvesting.Drops.RecordCount==0 && !Trees(world).Any(t=>Id(t)==expected.FelledId),"Second Continue cannot regenerate collected rewards"); await Remove(world);
    }
    // Read actual previous-revision files, copy them to an isolated root, then upgrade without touching the originals.
    // =========================================================
    private async Task ExternalLegacy(string path)
    {
        const string profile="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var oldStore=new CampaignStore(path); var previous=oldStore.Load(profile).Snapshot;
        Check(previous.SchemaVersion==2 && previous.Player.Inventory==null,"Actual pre-H4 revision has schema 2 without inventory");
        Check(previous.ContentFingerprint==CampaignSession.LegacyHarvestFingerprint(),"Frozen substitutions reproduce actual previous-revision fingerprint");
        CampaignSession.RequireContent(previous);
        CampaignSession.TestStorageRoot=Path.Combine(_root,"actual-previous-revision"); CampaignSession.Store().Save(previous,true);
        CampaignSession.Continue(previous); var world=await Spawn();
        Check(world.Blueprint.WorldId==previous.WorldId && world.MacroMap.Hash==previous.Blueprint.MapHash,"Actual old campaign retains frozen world identity/geography");
        var saved=world.Saves.Capture();
        Check(previous.Chunks.SelectMany(c=>c.Edits).All(edit=>saved.Chunks.Any(c=>c.Edits.Any(e=>e.Id==edit.Id && e.Removed==edit.Removed && e.BurnProgress==edit.BurnProgress))),"Old scenery removals/burn state retained");
        Check(saved.Player.Inventory.Items.Length==2,"Old campaign receives one authored starter loadout");
        Check(await world.Saves.SaveAsync(),"Actual old checkpoint upgrades successfully"); await Remove(world);
        var current=CampaignSession.Store().Load(profile).Snapshot;
        Check(current.SchemaVersion==3 && current.Player.Inventory.Items.Length==2,"Upgraded real disk checkpoint contains inventory");
        Check(oldStore.Load(profile).Snapshot.SchemaVersion==2,"Original previous-revision files left intact");
    }
    // Corrupt ownership/state must reject before a worker commit, without depending on live Resources.
    // =========================================================
    private void ValidationCases(CampaignSnapshot source)
    {
        var store=CampaignSession.Store(); store.Validate(source);
        Reject(()=>store.Validate(source with { Player=source.Player with { Inventory=null } }),"Missing schema-3 inventory rejects");
        var inventory=source.Player.Inventory;
        Reject(()=>store.Validate(source with { Player=source.Player with { Inventory=inventory with { HotbarKeys=new long[] { 99,0,0,0,0,0,0,0,0 } } } }),"Stale shortcuts reject");
        var owner=source.Chunks.First(c=>(c.Drops?.Length ?? 0)>0); var drop=owner.Drops[0];
        var duplicate=owner with { Drops=owner.Drops.Concat(new[] { drop }).ToArray() };
        Reject(()=>store.Validate(source with { Chunks=source.Chunks.Select(c=>ReferenceEquals(c,owner) ? duplicate : c).ToArray() }),"Duplicate drop IDs reject");
        var missing=owner with { Drops=owner.Drops.Select(d=>d with { SourceId="surface/0/0/unknown" }).ToArray() };
        Reject(()=>store.Validate(source with { Chunks=source.Chunks.Select(c=>ReferenceEquals(c,owner) ? missing : c).ToArray() }),"Drop without matching felled source rejects");
        var wrong=owner with { Drops=owner.Drops.Select(d=>d with { X=d.X+source.Blueprint.ChunkSize*2 }).ToArray() };
        Reject(()=>store.Validate(source with { Chunks=source.Chunks.Select(c=>ReferenceEquals(c,owner) ? wrong : c).ToArray() }),"Wrong landing chunk rejects");
        Reject(()=>store.Validate(source with { SchemaVersion=2 }),"Old schema cannot hide new inventory/drop data");
        Reject(()=>CampaignSession.RequireContent(source with { ContentFingerprint=CampaignSession.LegacyHarvestFingerprint() }),"Schema-3 cannot bypass current content fingerprint");
    }
    // Wait for real World arrival and its initial save; freeze test movement/enemies without disabling interaction contracts.
    // =========================================================
    private async Task<WorldScene> Spawn()
    {
        Input.UseAccumulatedInput=false;
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>(); AddChild(world);
        Check(world.StartupError==null,"World startup: "+world.StartupError);
        await Until(()=>world.Saves?.ReadyToSave==true && !world.Saves.Busy,"campaign arrival/save");
        Check(world.Saves.LastError=="","Initial save failed: "+world.Saves.LastError);
        world.Player.SetPhysicsProcess(false); world.TestActorsRoot.ProcessMode=ProcessModeEnum.Disabled; return world;
    }
    // Existing streamer owns the set of loaded trees; test-only enumeration is not a gameplay polling path.
    // =========================================================
    private static GameTree[] Trees(WorldScene world) => world.ChunkActorsRoot.FindChildren("*","StaticBody2D",true,false).OfType<GameTree>().Where(t=>!t.IsQueuedForDeletion() && t.CanProcess()).ToArray();
    private static string Id(GameTree tree)=>tree.GetMeta("world_instance_id").AsString();
    private static ChunkCoord IdOwner(CampaignSnapshot snapshot,string id) { var chunk=snapshot.Chunks.Single(c=>c.Edits.Any(e=>e.Id==id)); return new(chunk.X,chunk.Y); }
    private static float Edge(Vector2 p,int size) { float x=Mathf.PosMod(p.X,size),y=Mathf.PosMod(p.Y,size); return Mathf.Min(Mathf.Min(x,size-x),Mathf.Min(y,size-y)); }
    private static int Count(WorldScene world,string id)=>world.Harvesting.Inventory.Storage.Capture().Where(s=>s.ItemId==id).Sum(s=>s.Count);
    private void KeyPress(Key key) { Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=true }); Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=false }); Input.FlushBufferedEvents(); }
    private async Task Until(Func<bool> condition,string label) { var timer=System.Diagnostics.Stopwatch.StartNew(); while(!condition()) { if(timer.Elapsed.TotalSeconds>45) throw new TimeoutException(label); await Frames(1); } }
    private async Task Wait(double seconds)=>await ToSignal(GetTree().CreateTimer(seconds),SceneTreeTimer.SignalName.Timeout);
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private async Task Remove(WorldScene world) { world.QueueFree(); await Frames(4); }
    private void Reject(Action action,string message) { bool rejected=false; try { action(); } catch(Exception e) when(e is InvalidDataException || e is ArgumentException || e is IOException) { rejected=true; } Check(rejected,message); }
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
