// Exercise the real F9 menu, category exclusivity, fire hosting and reversible live combat settings.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateDebugHud : Node
{
    private int _checks;
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var movement=GD.Load<MovementMasterConfig>("res://ENTITIES/Shared/Movement/DefaultMovementConfig.tres");
            var original=movement.EngagementDefaults; float originalDistance=original.PreferredDistance;
            var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
            world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false;
            AddChild(world); await Frames(180);
            var owner=world.GetNode<EnvironmentDebugPanel>("DebugUIRoot/EnvironmentDebugPanel");
            var menu=owner.GetNode<DebugCategoryMenu>("CategoryMenu");
            var environment=(VBoxContainer)menu.FindChild("EnvironmentControls",true,false);
            var combat=(VBoxContainer)menu.FindChild("CombatControls",true,false);
            var diagnostics=owner.GetNode<Control>("WorldDiagnostics");
            Check(environment.IsVisibleInTree() && !combat.IsVisibleInTree(),"Initial category");
            Check(environment.FindChildren("*","Button",true,false).OfType<Button>().Any(b=>b.Text=="Ignite nearest tree"),"Fire outside Environment");
            menu.SelectCategory("Combat"); await Frames(2);
            Check(!environment.IsVisibleInTree() && combat.IsVisibleInTree() && !diagnostics.Visible,"Category exclusivity");
            Check(!owner.IsProcessing(),"Inactive Environment still polls");
            var toggles=combat.FindChildren("*","CheckButton",true,false).OfType<CheckButton>().ToArray();
            toggles.Single(t=>t.Text=="Engagement positioning").ButtonPressed=false;
            Check(!movement.EngagementEnabled,"Engagement widget binding");
            toggles.Single(t=>t.Text=="Knockback").ButtonPressed=false;
            Check(!GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres").KnockbackEnabled,"Combat widget binding");
            combat.FindChildren("*","HSlider",true,false).OfType<HSlider>().First().Value=40;
            Check(movement.EngagementDefaults.PreferredDistance==40 && original.PreferredDistance==originalDistance,"Slider changed authored default");
            owner._UnhandledInput(new InputEventKey { Keycode=Key.F9,Pressed=true }); await Frames(2);
            Check(!menu.Visible && !diagnostics.Visible && !combat.IsVisibleInTree(),"F9 did not hide all");
            Check(!owner.IsProcessing() && !combat.GetChild<CombatDebugPanel>(0).IsProcessing(),"Hidden HUD still polling");
            owner._UnhandledInput(new InputEventKey { Keycode=Key.F9,Pressed=true }); await Frames(2);
            Check(menu.Visible && combat.IsVisibleInTree() && !diagnostics.Visible,"F9 forgot selected category");
            menu.SelectCategory("Environment"); await Frames(2);
            Check(diagnostics.Visible && !combat.IsVisibleInTree(),"Return to Environment");
            menu.AddCategory("Future"); menu.SelectCategory("Future"); await Frames(2);
            Check(!environment.IsVisibleInTree() && !combat.IsVisibleInTree() && !diagnostics.Visible,"Future category exclusivity");
            menu.SelectCategory("Environment");
            GetWindow().Size=new Vector2I(800,600); await Frames(3);
            Check(menu.GetRect().End.Y<=GetViewport().GetVisibleRect().Size.Y && menu.GetRect().End.X<=GetViewport().GetVisibleRect().Size.X,"Menu outside resized viewport");
            var fire=world.GetNode<WorldFireDebugPanel>("DebugUIRoot/FireDebugPanel"); fire.IgniteNearest(); fire.ResetAll();
            Check(true,"Fire controls callable");
            world.QueueFree(); await Frames(4);
            Check(movement.EngagementDefaults==original && movement.EngagementEnabled,"Debug profile not restored");
            Check(GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres").KnockbackEnabled,"Combat switch leaked across sessions");
            GD.Print($"DEBUG HUD PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
