// P4 integration/storage acceptance fixture; explicitly run locally, never against real campaign storage.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FantasyGame.WorldInteractions.Fire;
public partial class ValidateCampaignSaves : Node
{
    private const string A = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", B = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private int _checks;
    private string _root;
    // Two commands exercise a real process boundary: -- --p4-write and -- --p4-read.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            _root = ProjectSettings.GlobalizePath("user://p4-validation"); CampaignSession.TestStorageRoot = _root;
            if (OS.GetCmdlineUserArgs().Contains("--p4-write")) await WritePhase();
            else if (OS.GetCmdlineUserArgs().Contains("--p4-read")) await ReadPhase();
            else throw new ArgumentException("Run with -- --p4-write first, then -- --p4-read in a new Godot process.");
            GD.Print($"P4 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // New campaigns share a seed but must never share WorldId or mutable records.
    // =========================================================
    private async Task WritePhase()
    {
        var a = await New(A); var player = a.Player;
        var clock = a.GetNode<EnvironmentRuntime>("SystemsRoot/Environment").Clock;
        clock.Config.Paused = true; clock.SetHour(3);
        var trees = Props(a).OfType<FantasyGame.World.Trees.Tree>().ToArray();
        Check(trees.Length >= 2,"Not enough trees in test seed");
        string removed = trees[0].GetMeta("world_instance_id").AsString();
        string burnt = trees[1].GetMeta("world_instance_id").AsString();
        trees[1].GetNode<Burnable>("Burnable").RestoreProgress(1); trees[0].QueueFree(); await Frames(2);
        foreach (var entity in a.EntitiesRoot.GetChildren().OfType<Entity>()) entity.TakeDamage(entity.Health);
        player.ReceiveDamage(new DamageRequest(13),EffectiveStats.Neutral);
        string aWorld = a.Blueprint.WorldId;
        Check(await a.Saves.SaveAsync(),"A save failed");
        var originalPosition = player.Position;
        player.Position = new Vector2(9000,5000); await Frames(260);
        Check(await a.Saves.SaveAsync(),"A distant save failed");
        Check(a.Stream.LoadedCount <= 81,"Unbounded streamed chunks");
        player.Position = originalPosition; await Frames(260);
        Check(!Props(a).Any(p => Id(p)==removed),"Removed tree returned after chunk reload");
        Check(Props(a).First(p => Id(p)==burnt).GetNode<Burnable>("Burnable").State == BurnState.Burnt,"Burn state lost after chunk reload");
        Check(await a.Saves.SaveAsync(),"A return save failed");
        var checkpointA = CampaignSession.Store().Load(A).Snapshot;
        var pendingA = a.Saves.SaveAsync();
        await Remove(a);
        var b = await New(B);
        Check(await pendingA,"Detached A write failed during scene switch");
        Check(CampaignSession.Store().Load(A).Snapshot.WorldId == aWorld,"Delayed A write changed campaign ownership");
        Check(b.Blueprint.Seed == checkpointA.Blueprint.Seed && b.Blueprint.WorldId != aWorld,"Same seed merged world identities");
        var bClock = b.GetNode<EnvironmentRuntime>("SystemsRoot/Environment").Clock; bClock.Config.Paused = true; bClock.SetHour(17);
        Check(Props(b).Any(p => Id(p)==removed),"A removal leaked into B");
        Check(Props(b).First(p => Id(p)==burnt).GetNode<Burnable>("Burnable").State == BurnState.Healthy,"A fire leaked into B");
        foreach (var entity in b.EntitiesRoot.GetChildren().OfType<Entity>()) entity.TakeDamage(1);
        b.Player.ReceiveDamage(new DamageRequest(1000000),EffectiveStats.Neutral);
        Check(await b.Saves.SaveAsync(),"B save failed"); await Remove(b);
        var store = CampaignSession.Store();
        Check(store.Load(A).Snapshot.Clock.Hour == 3 && store.Load(B).Snapshot.Clock.Hour == 17,"Clock isolation failed");
        CheckpointFiles.Write(Path.Combine(_root,"expectations.json"),new[] { removed,burnt,aWorld },1024*1024);
        StorageCases(checkpointA);
        // An SO1 campaign must restore unchanged after the layout-only SO2 refactor.
        var legacy = checkpointA with { SchemaVersion=1,Player=checkpointA.Player with { Health=null }, ContentFingerprint = CampaignSession.LegacySO1ContentFingerprint() };
        CampaignSession.RequireContent(legacy);
        CampaignSession.Continue(legacy);
        var migrated = await Spawn();
        Check(migrated.Player.HealthState.Current==migrated.Player.HealthState.Maximum,"Schema-1 HP default");
        Check(migrated.Saves.Capture().SchemaVersion==2 && migrated.Saves.Capture().Player.Health!=null,"Schema-2 capture missing HP");
        migrated.Player.ReceiveDamage(new DamageRequest(13),EffectiveStats.Neutral);
        Check(migrated.Blueprint.WorldId == legacy.WorldId && migrated.MacroMap.Hash == legacy.Blueprint.MapHash,"SO1 compatibility bridge changed world identity");
        Check(!Props(migrated).Any(p => Id(p)==removed),"SO1 compatibility bridge resurrected an edit");
        Check(migrated.Saves.Capture().ContentFingerprint == CampaignSession.ContentFingerprint(),"SO2 checkpoint did not adopt verified current fingerprint");
        Check(await migrated.Saves.SaveAsync(),"Verified legacy campaign could not commit an SO2 checkpoint");
        var committedMigration = CampaignSession.Store().Load(A).Snapshot;
        Check(committedMigration.ContentFingerprint == CampaignSession.ContentFingerprint()
            && committedMigration.WorldId == legacy.WorldId && committedMigration.Blueprint.MapHash == legacy.Blueprint.MapHash
            && committedMigration.Chunks.Any(c => c.Edits.Any(e => e.Id == removed && e.Removed)),
            "Committed layout migration lost its fingerprint, identity, map or removal");
        await Remove(migrated);
    }
    // Restart must restore exact blueprint/edits rather than choose a fresh random seed.
    // =========================================================
    private async Task ReadPhase()
    {
        string[] expected = CheckpointFiles.Read<string[]>(Path.Combine(_root,"expectations.json"),1024*1024);
        foreach (string profile in new[] { A,B,A })
        {
            var loaded = CampaignSession.Store().Load(profile); CampaignSession.Continue(loaded.Snapshot);
            var world = await Spawn();
            Check(world.Blueprint.WorldId == loaded.Snapshot.WorldId,"Wrong world after restart/profile switch");
            Check(world.MacroMap.Hash == loaded.Snapshot.Blueprint.MapHash && world.FeaturePlan.Hash == loaded.Snapshot.Blueprint.FeatureHash,"Frozen geography changed");
            var pos = world.Player.Position;
            Check(world.Player.HealthState.Current == (loaded.Snapshot.Player.Health ?? world.Player.HealthState.Maximum),"Player HP changed after restart");
            Check(world.Player.IsPhysicsProcessing() == world.Player.HealthState.IsAlive,"Restored player movement state");
            Check(pos == new Vector2(loaded.Snapshot.Player.X,loaded.Snapshot.Player.Y),"Player location changed");
            Check(world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment").Clock.Hour == (profile==A ? 3 : 17),"Inactive clock advanced");
            Check(Props(world).Any(p => Id(p)==expected[0]) == (profile==B),"Wrong per-profile removed state");
            Check(Props(world).First(p => Id(p)==expected[1]).GetNode<Burnable>("Burnable").State == (profile==A ? BurnState.Burnt : BurnState.Healthy),"Wrong per-profile fire state");
            var actors = world.EntitiesRoot.GetChildren().OfType<Entity>().ToArray();
            Check(profile==A ? actors.Length==0 : actors.Length==1 && actors[0].Health==loaded.Snapshot.TestEntity.Health,"Test actor death/health did not persist");
            Check(world.Stream.WorldId == loaded.Snapshot.WorldId && world.Stream.LoadedCount <= 81,"Wrong stream identity or bounds");
            await Remove(world);
        }
    }
    // Corrupt/interrupted writes, safe paths, unsupported versions, and ownership mismatches are independent cases.
    // =========================================================
    private void StorageCases(CampaignSnapshot source)
    {
        var store = new CampaignStore(Path.Combine(_root,"recovery-" + Guid.NewGuid().ToString("N")));
        store.Save(source,true); store.Save(source with { Clock = new SavedClock(8,600,true) });
        var key = new ActiveCampaign(source.ProfileId,source.RunId,source.WorldId);
        string path = Path.Combine(store.RunPath(key),"checkpoint.json");
        File.WriteAllText(path + ".tmp","interrupted");
        Check(store.Load(source.ProfileId).Snapshot.Clock.Hour == 8,"Uncommitted temp replaced a valid save");
        File.WriteAllText(path,"corrupt"); var recovered = store.Load(source.ProfileId);
        Check(recovered.Recovered && recovered.Snapshot.Clock.Hour == source.Clock.Hour,"Previous checkpoint recovery failed");
        store.Save(recovered.Snapshot); File.WriteAllText(path,"corrupt again");
        Check(store.Load(source.ProfileId).Recovered,"Recovery followed by another save destroyed the backup");
        Reject(() => store.Save(source with { WorldId = Guid.NewGuid().ToString("N") }),"Ownership mismatch accepted");
        Reject(() => store.Save(source with { Player=source.Player with { Health=null } }),"Schema-2 missing HP accepted");
        Reject(() => store.Save(source with { Player=source.Player with { Health=-1 } }),"Negative player HP accepted");
        Reject(() => store.Save(source with { SchemaVersion = 999 }),"Unsupported schema accepted");
        Reject(() => store.Save(source with { Player = source.Player with { Layer = "unimplemented-cave" } }),"Unsupported layer accepted");
        Reject(() => store.Save(source with { Player = source.Player with { X = float.NaN } }),"Nonfinite position accepted");
        Reject(() => store.ProfilePath("../../escape"),"Unsafe profile path accepted");
        Reject(() => store.Save(source with { Chunks = source.Chunks.Concat(source.Chunks).ToArray() }),"Duplicated edits accepted");
        Reject(() => CampaignSession.RequireContent(source with { ContentFingerprint = new string('0',64) }),"Content mismatch accepted");
        string active = Path.Combine(store.ProfilePath(source.ProfileId),"active.json");
        // Creating a second active-pointer checkpoint supplies an independent pointer backup.
        store.Save(source,true); File.WriteAllText(active,"bad pointer");
        Check(store.Load(source.ProfileId).Recovered,"Active pointer backup was not recovered");
    }
    private async Task<WorldScene> New(string profile) { CampaignSession.New(profile,123); return await Spawn(); }
    private async Task<WorldScene> Spawn()
    {
        var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>(); AddChild(world);
        Check(world.StartupError == null,"World startup failed: " + world.StartupError);
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (world.Saves == null || !world.Saves.ReadyToSave || world.Saves.Busy)
        { if (timeout.Elapsed.TotalSeconds > 30) throw new TimeoutException("Campaign startup/save timed out"); await Frames(1); }
        Check(world.Saves != null && world.Saves.ReadyToSave,"Campaign arrival never became ready");
        while (world.Saves.Busy) await Frames(1);
        Check(string.IsNullOrEmpty(world.Saves.LastError),"Initial campaign checkpoint failed"); return world;
    }
    private async Task Remove(WorldScene world) { world.QueueFree(); await Frames(3); }
    private async Task Frames(int count) { for (int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private static Node2D[] Props(WorldScene world) => world.ActorsRoot.FindChildren("*","",true,false).OfType<Node2D>().Where(p => p.HasMeta("world_instance_id")).ToArray();
    private static string Id(Node2D prop) => prop.GetMeta("world_instance_id").AsString();
    private void Check(bool value,string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private void Reject(Action action,string message)
    { bool rejected = false; try { action(); } catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException) { rejected = true; } Check(rejected,message); }
}
