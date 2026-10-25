// Item/catalog/storage invariants plus real Player/HUD practice wiring. Never reads or writes campaign files.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class ValidateItemInventory : Node
{
    private int _checks;
    // Validate authoring failures, atomic transactions and actual input/equipment/UI integration.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var catalog=GD.Load<ItemMasterConfig>("res://ITEMS/DefaultItemConfig.tres").Catalog; catalog.Initialize();
            var log=catalog.Get("wooden_log"); var axe=catalog.Get("starteraxe"); var sword=catalog.Get("startersword");
            Check(log!=null && axe!=null && sword!=null,"Three registered examples");
            Check(log.Equipment==null && log.Icon!=null && log.MaxStack==40,"Ordinary stackable material");
            Check(axe.Equipment==GD.Load<EquippedItemDefinition>("res://ITEMS/Tools/Axes/StarterAxe.tres"),"Existing combat resource reused");
            var invalid=new ItemCatalog { Items=new() { log,log } }; Rejects(invalid.Initialize,"Duplicate IDs rejected"); Check(invalid.Get(log.Id)==null,"No partial lookup survives failure");
            var bad=new ItemDefinition { Id="bad",WeightKg=float.NaN }; Rejects(bad.Validate,"Nonfinite mass rejected");
            var store=new InventoryStorage(catalog,2); int events=0; store.Changed+=()=>events++;
            Check(store.TryAdd(log,45) && store.Get(0).Count==40 && store.Get(1).Count==5,"Splits into bounded stacks");
            Check(!store.TryAdd(log,36) && events==1 && store.Get(1).Count==5,"Rejected add atomic and silent");
            Check(!store.TryAdd(new ItemDefinition { Id=log.Id },1),"Conflicting resource cannot spoof canonical ID");
            Check(!store.TryAdd(log,int.MaxValue) && !store.TryAdd(log,0),"Overflow/zero quantities rejected");
            Check(!store.TryRemove(-1,1) && !store.TryRemove(0,41),"Invalid removals atomic");
            Check(store.TotalWeightKg()==45,"Weight reflects actual contents");
            using(var belt=new InventoryHotbar(store,9))
            {
                Check(belt.Bind(0,1) && belt.Get(0).Count==5,"Hotbar references storage");
                Check(store.TryRemove(0,10) && store.TryMove(1,0),"Merge accepted");
                Check(belt.Get(0).IsEmpty && store.Get(0).Count==35,"Fully merged source reference clears without duplicate items");
                Check(belt.Bind(0,0) && store.TryMove(0,1) && belt.Get(0).Count==35,"Shortcut follows moved stack identity");
                belt.CanSelect=_=>false; Check(!belt.Select(0) && belt.SelectedSlot==-1,"Rejected equipment selection not queued");
                belt.CanSelect=_=>true; Check(belt.Select(0),"Accepted selection");
                int invalidated=0; belt.SelectionInvalidated+=()=>invalidated++;
                Check(store.TryRemove(1,35) && invalidated==1 && belt.Get(0).IsEmpty,"Selected removed stack invalidated once");
                Check(store.TryAdd(log,1) && belt.Get(0).IsEmpty,"New stack cannot inherit old shortcut");
            }
            Input.UseAccumulatedInput=false;
            for(int session=0;session<2;session++)
            {
                var practice=GD.Load<PackedScene>("res://TESTS/Inventory/InventoryPractice.tscn").Instantiate<InventoryPractice>(); AddChild(practice); await Frames(4);
                var tool=practice.Player.GetNode<EquippedTool>("EquippedAxe"); var input=practice.Player.GetNode<PlayerInput>("Input");
                Check(practice.Storage.Get(2).Count==12 && practice.Hotbar.Get(2).Count==12,"Fixture has real log stack");
                Check(tool.EquippedItem==axe.Equipment,"Initial axe selection");
                input._UnhandledInput(new InputEventKey { Keycode=Key.Shift,PhysicalKeycode=Key.Shift,Location=KeyLocation.Right,Pressed=true });
                Check(input.SprintHeld,"Hotbar mode preserves sprint input");
                input._Input(new InputEventKey { Keycode=Key.Shift,PhysicalKeycode=Key.Shift,Location=KeyLocation.Right,Pressed=false });
                Check(!input.SprintHeld,"Sprint release still works");
                KeyPress(Key.Key2); Check(tool.EquippedItem==sword.Equipment && practice.Hotbar.SelectedSlot==1,"Input selects real sword");
                input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
                Check(tool.Attack.Busy,"Real weapon attacks"); KeyPress(Key.Key1);
                Check(tool.EquippedItem==sword.Equipment && practice.Hotbar.SelectedSlot==1,"Busy switch rejected atomically");
                await ToSignal(GetTree().CreateTimer(1.5),SceneTreeTimer.SignalName.Timeout);
                KeyPress(Key.Key3); Check(tool.EquippedItem==null && practice.Hotbar.SelectedSlot==2,"Material selection has empty hands");
                input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100); Check(!tool.Attack.Busy,"Material never attacks");
                KeyPress(Key.I); Check(input.InventoryOpen && !GetTree().Paused,"Nonpausing populated inventory"); KeyPress(Key.Key1);
                Check(tool.EquippedItem==null && practice.Hotbar.SelectedSlot==2,"Inventory blocks selection");
                Check(practice.Storage.TryAdd(log,5) && practice.Hotbar.Get(2).Count==17,"Changed quantity reaches hotbar");
                var view=practice.FindChildren("*","Control",true,false).OfType<PlayerInventoryView>().Single();
                Check(view.FindChildren("*","Label",true,false).OfType<Label>().Any(l=>l.Text=="17"),"Visible inventory refreshes count");
                KeyPress(Key.Escape); KeyPress(Key.Key1); Check(tool.EquippedItem==axe.Equipment,"Close restores selection");
                input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
                Check(practice.Storage.TryRemove(0,1) && tool.EquippedItem==null && tool.Attack.Busy,"Lost weapon stops contacts without refunding cooldown");
                KeyPress(Key.Key2); Check(tool.EquippedItem==null,"Cannot bypass lost weapon recovery");
                Check(!view.IsProcessing(),"No inventory idle callback");
                practice.QueueFree(); await Frames(3);
            }
            GD.Print($"ITEM INVENTORY PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Inject actual input before GUI/unhandled stages.
    // =========================================================
    private void KeyPress(Key key)
    { Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=true }); Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=false }); Input.FlushBufferedEvents(); }
    // Await scene/layout cleanup without blocking the engine.
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    // Expected authoring failures must fail explicitly.
    // =========================================================
    private void Rejects(Action action,string message) { bool rejected=false; try { action(); } catch(InvalidOperationException) { rejected=true; } Check(rejected,message); }
    // Report the first violated contract precisely.
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
