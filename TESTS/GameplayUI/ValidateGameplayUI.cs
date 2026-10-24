// Integration checks for the real player/input/HUD contracts without campaign disk writes or world generation.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class ValidateGameplayUI : Node
{
    private int _checks;
    // Exercise input propagation, held-state cleanup, viewport layout and repeated scene lifecycle.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            Input.UseAccumulatedInput=false;
            var escapeProbe=new GameplayUIEscapeProbe(); AddChild(escapeProbe);
            var authored=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate();
            Check(authored.GetNodeOrNull<GameplayHUD>("GameplayUIRoot")!=null,"World owns shared HUD"); authored.Free();
            for (int session=0;session<2;session++)
            {
                var scene=new Node2D(); AddChild(scene);
                var player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); player.Name="Player"; scene.AddChild(player);
                var hud=GD.Load<PackedScene>("res://UI/HUD/Gameplay/GameplayHUD.tscn").Instantiate<GameplayHUD>(); hud.PlayerPath=new NodePath("../Player"); scene.AddChild(hud);
                await Frames(3);
                var input=player.GetNode<PlayerInput>("Input");
                var inventory=hud.FindChildren("*","Control",true,false).OfType<PlayerInventoryView>().Single();
                var hotbar=hud.FindChildren("*","Control",true,false).OfType<GameplayHotbar>().Single();
                Check(hotbar.FindChildren("*","Control",true,false).OfType<InventorySlot>().Count()==9,"Nine hotbar slots");
                Check(!inventory.Visible && !input.InventoryOpen,"Initially closed");
                Check(!hud.IsProcessing() && !hotbar.IsProcessing() && !inventory.IsProcessing(),"No idle UI process callbacks");
                int selections=0,attacks=0; input.StarterSelectionRequested+=_=>selections++; input.ToolUseRequested+=_=>attacks++;
                KeyPress(Key.Key1); await Frames(1); Check(selections==1,"Starter selection preserved");
                input._UnhandledInput(new InputEventKey { Keycode=Key.Shift,PhysicalKeycode=Key.Shift,Location=KeyLocation.Right,Pressed=true });
                player.TryGuard(Vector2.Right); Check(input.SprintHeld && player.Guard.Active,"Held state arranged");
                Input.ActionPress("player_right"); KeyPress(Key.I);
                Check(inventory.Visible && input.InventoryOpen && !GetTree().Paused,"I opens without pausing");
                Check(!input.SprintHeld && !player.Guard.Active && input.ReadMovement()==Vector2.Zero,"Held state and movement suppressed");
                KeyPress(Key.Key1); Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex=MouseButton.Left,Pressed=true,Position=new Vector2(10,10) });
                Check(selections==1 && attacks==0,"Inventory prevents combat/number-key leaks");
                Vector2 position=player.GlobalPosition; await Frames(6);
                Check(player.GlobalPosition.IsEqualApprox(position),"Held arrows cannot move through overlay");
                Check(!GetTree().Paused && player.IsPhysicsProcessing(),"World/player physics stays active");
                int escapes=escapeProbe.Escapes; KeyPress(Key.Escape); Check(escapeProbe.Escapes==escapes,"Open inventory consumes Esc before campaign menu"); Check(!inventory.Visible && !input.InventoryOpen && !GetTree().Paused,"Esc closes first");
                Check(input.ReadMovement().X>0,"Movement restored after close"); Input.ActionRelease("player_right");
                KeyPress(Key.I); KeyPress(Key.I); Check(!inventory.Visible,"I closes with button focus");
                KeyPress(Key.I); inventory.FindChildren("*","Button",true,false).OfType<Button>().Single().EmitSignal(BaseButton.SignalName.Pressed);
                Check(!input.InventoryOpen && !inventory.Visible,"Factory close button restores input");
                foreach (var size in new[] { new Vector2I(1920,1080),new Vector2I(800,600),new Vector2I(480,360) })
                {
                    GetWindow().Size=size; await Frames(4);
                    Vector2 viewport=GetViewport().GetVisibleRect().Size;
                    Check(hotbar.Position.X>=0 && hotbar.Position.X+hotbar.Size.X*hotbar.Scale.X<=viewport.X+1,"Hotbar safe at "+size);
                    KeyPress(Key.I); await Frames(2);
                    var panel=inventory.GetChildren().OfType<PanelContainer>().Single();
                    Check(panel.Position.X>=0 && panel.Position.Y>=0 && panel.GetRect().End.X<=viewport.X+1 && panel.GetRect().End.Y<=viewport.Y+1,"Inventory safe at "+size);
                    KeyPress(Key.Escape);
                }
                KeyPress(Key.I); hud.QueueFree(); await Frames(3); Check(!input.InventoryOpen,"HUD teardown releases suppression");
                scene.QueueFree(); await Frames(3);
            }
            GD.Print($"GAMEPLAY UI PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Inject actual viewport input, including key release, rather than calling view methods directly.
    // =========================================================
    private void KeyPress(Key key)
    { Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=true }); Input.ParseInputEvent(new InputEventKey { Keycode=key,PhysicalKeycode=key,Pressed=false }); Input.FlushBufferedEvents(); }
    // Allow deferred layout and physics between state transitions.
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    // Fail with the exact broken interaction contract.
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}

// Mirrors the campaign controller’s unhandled-key stage to verify inventory Esc priority.
public partial class GameplayUIEscapeProbe : Node
{
    public int Escapes { get; private set; }
    // Count only events that survive PlayerInput's earlier inventory handling.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent input)
    { if (input is InputEventKey key && key.Pressed && key.Keycode==Key.Escape) Escapes++; }
}
