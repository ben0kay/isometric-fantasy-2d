// C2.5 integration: real input adapters, action priority, shared damage/guard math, visuals and save-neutral definitions.
using Godot;
using System;
using System.Threading.Tasks;
public partial class ValidatePlayerActions : Node2D
{
    private PlayerMovement _player;
    private Entity _target;
    private PlayerInput _input;
    private EquippedTool _tool;
    private int _checks;
    // Isolated production actors; no campaign disk storage or WorldScene required.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            _player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>();
            _player.Config=(PlayerMasterConfig)_player.Config.Duplicate(); AddChild(_player);
            _input=_player.GetNode<PlayerInput>("Input"); _tool=_player.GetNode<EquippedTool>("EquippedAxe");
            _target=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
            var definition=(EntityDefinition)GD.Load<EntityDefinition>("res://ENTITIES/Species/Undead/Skeletons/Basic/SkeletonBasic.tres").Duplicate();
            definition.MaxHealth=50; definition.ChasePlayer=false; _target.Configure(definition); _target.Position=new Vector2(40,0); AddChild(_target);
            await Frames(3); await Attacks(); await GuardAndMovement(); await HopAndPriority(); await Cleanup();
            _player.QueueFree(); if (GodotObject.IsInstanceValid(_target)) _target.QueueFree(); await Frames(3);
            GD.Print($"C2.5 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // Primary and alternate share cooldown but have separate values, geometry and generic presentation.
    // =========================================================
    private async Task Attacks()
    {
        Check(_tool.EquippedItem.Id=="starteraxe" && !_tool.ShieldEquipped,"Startup selection");
        Check(!_tool.IsPhysicsProcessing(),"Idle attack physics");
        _input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        Check(_tool.IsAttacking,"Primary request");
        _input.EmitSignal(PlayerInput.SignalName.AlternateUseRequested,Vector2.Right*100);
        Select(1); Check(_tool.EquippedItem.Id=="starteraxe","Switch bypassed committed attack");
        await Wait(0.65); Check(_target.Health==39 && !_tool.Attack.Busy,"Primary damage/duplicate contact");
        _input.EmitSignal(PlayerInput.SignalName.AlternateUseRequested,Vector2.Right*100);
        await Wait(0.12); Check(Mathf.Abs(_tool.Rotation)<0.01f,"Thrust rotated away from aim");
        await Wait(0.7); Check(_target.Health==34,"Alternate damage");
        Select(1); Check(_tool.EquippedItem.Id=="startersword","Sword selection");
        Check(!_tool.EquippedItem.PrimaryAttack.HitTools && !_tool.EquippedItem.SecondaryAttack.HitTools,"Sword chops props");
        _input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        await Wait(0.6); Check(_target.Health==26,"Sword primary");
        _input.EmitSignal(PlayerInput.SignalName.AlternateUseRequested,Vector2.Right*100);
        await Wait(0.7); Check(_target.Health==20,"Sword secondary");
        _target.Stats.SetModifier("armor",new(CombatStat.Defense,Flat:2)); Select(0);
        _input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        await Wait(0.65); Check(_target.Health==11,"Flat defense lost"); _target.Stats.RemoveModifier("armor");
        Check(!_tool.IsPhysicsProcessing(),"Settled attack callback");
        Check(GD.Load<EntityDefinition>("res://ENTITIES/Species/Undead/Skeletons/Basic/SkeletonBasic.tres").MaxHealth==3,"Production skeleton changed");
    }
    // Shield state is independent of main-hand selection; frontal reduction follows armor and rounds once.
    // =========================================================
    private async Task GuardAndMovement()
    {
        Select(2); Check(_tool.ShieldEquipped,"Shield toggle");
        Utility(true); _player.Guard.Face(Vector2.Right);
        Check(_player.Guard.Active && _player.Actions.Current==PlayerAction.Block,"Guard action");
        _player.Stats.SetModifier("armor",new(CombatStat.Defense,Flat:2));
        var result=_player.ReceiveDamage(new DamageRequest(11,new Vector2(40,0)),EffectiveStats.Neutral);
        Check(result.AppliedDamage==4 && result.Blocked,"Armor then guard then rounding");
        _player.Guard.Face(Vector2.Right);
        result=_player.ReceiveDamage(new DamageRequest(11,new Vector2(-40,0)),EffectiveStats.Neutral);
        Check(result.AppliedDamage==9 && !result.Blocked,"Rear block");
        result=_player.ReceiveDamage(new DamageRequest(11),EffectiveStats.Neutral);
        Check(result.AppliedDamage==9 && !result.Blocked,"Unknown-origin invulnerability");
        _player.Stats.SetModifier("armor",new(CombatStat.Defense,Flat:100)); _player.Guard.Face(Vector2.Right);
        result=_player.ReceiveDamage(new DamageRequest(11,new Vector2(40,0)),EffectiveStats.Neutral);
        Check(result.AppliedDamage==0 && !result.Blocked,"Armor-only absorption reported a guard reduction");
        _player.Stats.RemoveModifier("armor");
        _input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        Check(!_tool.Attack.Busy,"Attack while guarding");
        Input.ActionPress("player_right");
        _input._UnhandledInput(new InputEventKey { Keycode=Key.Shift,Location=KeyLocation.Right,Pressed=true });
        await Frames(3);
        Check(Mathf.IsEqualApprox(_player.Velocity.X,_player.Config.WalkSpeed*_player.Guard.Profile.MovementMultiplier),"Guard failed to suppress sprint");
        // A right-button release reaches input even when UI would consume unhandled events.
        _input._Input(new InputEventMouseButton { ButtonIndex=MouseButton.Right,Pressed=false });
        Check(!_player.Guard.Active,"RMB release retained block"); await Frames(3);
        Check(Mathf.IsEqualApprox(_player.Velocity.X,_player.Config.WalkSpeed*_player.Config.SprintMultiplier),"Sprint did not recover");
        Input.ActionRelease("player_right"); _input.ResetMovementActions(); _player.Position=Vector2.Zero; await Frames(3);
        Utility(true); _input.EmitSignal(PlayerInput.SignalName.DodgeRequested,Vector2.Up);
        Check(!_player.Guard.Active && _player.IsDodging,"Dodge did not cancel guard");
        Utility(true); _input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        Check(!_player.Guard.Active && !_tool.Attack.Busy,"Dodge accepted block/attack");
        await Wait(0.25); _player.Position=Vector2.Zero;
        Utility(true); _input.ResetMovementActions(); Check(!_player.Guard.Active,"Focus/held reset retained guard");
        Select(2); Check(!_tool.ShieldEquipped,"Shield unequip");
    }
    // Hop lifts only rendered artwork; actions remain mutually exclusive and grant no invulnerability.
    // =========================================================
    private async Task HopAndPriority()
    {
        var sprite=_player.GetNode<Sprite2D>("Sprite"); Vector2 offset=sprite.Offset, position=_player.Position;
        var collider=_player.GetNode<CollisionShape2D>("Collision"); Transform2D shape=collider.Transform;
        float height=_player.Config.HopHeight; _player.Config.HopHeight=0;
        Utility(true); Check(!_player.IsHopping && _player.Actions.Current==PlayerAction.Idle,"Zero-height hop kept simulating/locking actions");
        _player.Config.HopHeight=height;
        Utility(true); Check(_player.IsHopping,"No-shield RMB hop");
        Utility(true); _input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        await Wait(0.12);
        Check(_player.HopHeight>0 && sprite.Offset.Y<offset.Y,"Hop visual did not lift");
        Check(_player.Position==position && collider.Transform==shape,"Hop moved body/collider");
        Check(!_tool.Attack.Busy,"Hop accepted attack");
        var result=_player.ReceiveDamage(new DamageRequest(5,new Vector2(40,0)),EffectiveStats.Neutral);
        Check(result.AppliedDamage==5 && !result.Blocked,"Hop granted immunity");
        await Wait(0.35); Check(!_player.IsHopping && sprite.Offset==offset,"Hop failed to restore artwork");
        Utility(true); Check(!_player.IsHopping,"Hop spam bypassed cooldown");
        await Wait(0.5);
        _input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        Utility(true); _input.EmitSignal(PlayerInput.SignalName.DodgeRequested,Vector2.Up);
        Check(!_player.IsHopping && !_player.IsDodging && _tool.IsAttacking,"Committed attack cancelled by utility/dodge");
        await Wait(0.65);
    }
    // Death and disabled ownership clear all transient actions, pending contacts and held mitigation.
    // =========================================================
    private async Task Cleanup()
    {
        Select(2); Utility(true);
        GetTree().Paused=true;
        Check(!_player.Guard.Active,"Pause retained a held guard");
        GetTree().Paused=false; Utility(true);
        _player.HealthState.Damage(1000);
        Check(!_player.Guard.Active && _player.Actions.Current==PlayerAction.Idle && !_tool.Attack.Busy,"Death cleanup");
        Utility(true); Check(!_player.Guard.Active,"Dead guard");
        _player.HealthState.Restore(100); await Frames(2); Utility(true);
        _player.ProcessMode=ProcessModeEnum.Disabled;
        Check(!_player.Guard.Active && _player.Actions.Current==PlayerAction.Idle,"Suspension cleanup");
        _player.ProcessMode=ProcessModeEnum.Inherit; await Frames(2);
        Check(!_tool.IsPhysicsProcessing(),"Reactivation replayed attack");
    }
    // Test the same public input event contracts that mouse/keyboard dispatch uses.
    // =========================================================
    private void Select(int slot) => _input._UnhandledInput(new InputEventKey {
        PhysicalKeycode=slot==0 ? Key.Key1 : slot==1 ? Key.Key2 : Key.Key3,Pressed=true });
    // Utility press/release is centralized in PlayerInput, not separate button polling in equipment.
    // =========================================================
    private void Utility(bool held) => _input.EmitSignal(PlayerInput.SignalName.UtilityRequested,held,Vector2.Right*100);
    // Real timers allow attack and cosmetic callbacks to finish at runtime.
    // =========================================================
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds),SceneTreeTimer.SignalName.Timeout);
    // Fixed-step synchronization makes actual movement/contact assertions reproducible.
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // Fail the engine process on the first violated contract.
    // =========================================================
    private void Check(bool value,string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
}
