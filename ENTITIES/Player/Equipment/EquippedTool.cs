// Shared main-hand presentation/intent adapter: independent item profiles, one C2 executor, optional shield flag.
using Godot;
public partial class EquippedTool : Node2D
{
    [Export] public AxeConfig Config { get; set; } // Existing scene reference remains compatible; owns hand/aim tuning.
    public AttackExecution Attack { get; private set; }
    public EquippedItemDefinition EquippedItem { get; private set; }
    public bool ShieldEquipped { get; private set; }
    public bool IsAttacking => Attack?.IsSwinging==true;
    private EquippedItemDefinition _axe, _sword;
    private Sprite2D _playerSprite, _toolSprite;
    private PlayerInput _input;
    private PlayerMovement _owner;
    private float _angle, _attackStart, _contactProgress, _sweepDegrees, _thrustPixels;
    private bool _visualSwing, _marker;
    private AttackPresentation _presentation;

    #region Intent and lifecycle
    // Load shared immutable definitions once; keep one held sprite and one executor across both weapons.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new AxeConfig(); _owner=GetParent<PlayerMovement>(); Attack=new AttackExecution(_owner);
        _playerSprite=_owner.GetNode<Sprite2D>("Sprite"); _toolSprite=GetNode<Sprite2D>("ToolSprite");
        _axe=_owner.Config.StarterAxe ?? GD.Load<EquippedItemDefinition>("res://ITEMS/Tools/Axes/StarterAxe.tres");
        _sword=_owner.Config.StarterSword ?? GD.Load<EquippedItemDefinition>("res://ITEMS/Weapons/Swords/Starter/StarterSword.tres");
        _input=_owner.GetNode<PlayerInput>("Input");
        _input.ToolUseRequested+=Primary; _input.AlternateUseRequested+=Secondary;
        _input.UtilityRequested+=Utility; _input.StarterSelectionRequested+=SelectStarter;
        _owner.HealthState.Changed+=OnHealthChanged;
        Equip(_axe); ProcessPriority=5; SetPhysicsProcess(false);
    }
    // The original primary request remains compatible with C2 callers.
    // =========================================================
    private void Primary(Vector2 aim) => BeginSwing(EquippedItem.PrimaryAttack,aim);
    // Alternate intent uses an independent resource through the exact same executor/cooldown.
    // =========================================================
    private void Secondary(Vector2 aim) => BeginSwing(EquippedItem.SecondaryAttack,aim);
    // No queued input: block/hop/dodge exclude new attacks, and the committed swing must finish first.
    // =========================================================
    private void BeginSwing(AttackProfile profile, Vector2 aim)
    {
        if (!Config.Enabled || Attack.Busy || !_owner.HealthState.IsAlive || !_owner.Actions.Begin(PlayerAction.Attack)) return;
        if (aim.LengthSquared()<0.01f) aim=Vector2.FromAngle(_angle);
        if (!Attack.TryStart(profile,aim)) { _owner.Actions.End(PlayerAction.Attack); return; }
        _thrustPixels=profile.ThrustPixels;
        _presentation=profile.Presentation; _contactProgress=profile.ContactProgress; _sweepDegrees=profile.SweepDegrees;
        _attackStart=Attack.Aim.Angle(); _visualSwing=true; SetPhysicsProcess(true);
    }
    // Shield release is unconditional; a rejected press is never queued for later activation.
    // =========================================================
    private void Utility(bool held, Vector2 aim)
    {
        if (!held) { _owner.StopGuard(); return; }
        if (ShieldEquipped) _owner.TryGuard(aim); else _owner.TryHop();
    }
    // Test selection is direct and cannot bypass a weapon's outstanding cooldown.
    // =========================================================
    private void SelectStarter(int slot)
    {
        if (!_owner.HealthState.IsAlive || Attack.Busy || _owner.Actions.Current!=PlayerAction.Idle) return;
        if (slot==0) Equip(_axe); else if (slot==1) Equip(_sword); else if (slot==2) ShieldEquipped=!ShieldEquipped;
    }
    // Geometry and identity come from item data, never axe/sword branches in attack execution.
    // =========================================================
    private void Equip(EquippedItemDefinition item)
    {
        EquippedItem=item; _toolSprite.Texture=item.Texture; _toolSprite.Centered=item.Centered;
        _toolSprite.Offset=item.GripOffset; _toolSprite.Scale=item.SpriteScale;
        _toolSprite.RotationDegrees=item.SpriteRotationDegrees;
    }
    // Death cancels committed state and sleeps presentation; restoring HP never replays an attack.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    { if (!_owner.HealthState.IsAlive) { CancelAttack(); Hide(); } SetProcess(_owner.HealthState.IsAlive); }
    // Disabled scene ownership must not retain pending contact or guard state.
    // =========================================================
    public override void _Notification(int what) { if (what==NotificationDisabled) CancelAttack(); }
    // Cancel only this adapter's action, preserving the single action owner's precedence rules.
    // =========================================================
    private void CancelAttack()
    {
        Attack?.Cancel(); _visualSwing=false; _owner?.Actions.End(PlayerAction.Attack); _owner?.StopGuard(); SetPhysicsProcess(false);
        if (_marker) { _marker=false; QueueRedraw(); }
    }
    // Release intent/health subscriptions and native contact objects on unload.
    // =========================================================
    public override void _ExitTree()
    {
        _owner.HealthState.Changed-=OnHealthChanged;
        if (GodotObject.IsInstanceValid(_input))
        {
            _input.ToolUseRequested-=Primary; _input.AlternateUseRequested-=Secondary;
            _input.UtilityRequested-=Utility; _input.StarterSelectionRequested-=SelectStarter;
        }
        Attack.Dispose();
    }
    #endregion

    #region Contact and held presentation
    // Fixed contact timing wakes only for accepted attacks and sleeps when the common cooldown ends.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        if (!Config.Enabled) { CancelAttack(); return; }
        using var trace=RuntimeFrameTrace.Measure("EquippedTool.AttackPhysics");
        Attack.Advance(delta);
        if (!Attack.IsSwinging) _owner.Actions.End(PlayerAction.Attack);
        SetPhysicsProcess(Attack.Busy);
    }
    // Sweep crosses forward aim at contact; thrust reaches maximum extension at its own contact sample.
    // =========================================================
    public override void _Process(double delta)
    {
        using var trace=RuntimeFrameTrace.Measure("EquippedTool.Process");
        Visible=Config.Enabled;
        if (!Visible) { CancelAttack(); return; }
        Position=Config.HandAnchor+_playerSprite.Position+new Vector2(0,-_owner.HopHeight);
        Vector2 aim=_input.ReadToolAim(_owner.GlobalPosition);
        float orbit=Mathf.Max(4,Config.OrbitRadius);
        if (_visualSwing)
        {
            float p=Attack.Progress, c=Mathf.Clamp(_contactProgress,0.001f,0.999f);
            float phase=p<=c ? p/c : 1-(p-c)/(1-c);
            if (_presentation==AttackPresentation.Thrust) { _angle=_attackStart; orbit+=Mathf.Clamp(phase,0,1)*_thrustPixels; }
            else
            {
                float t=p<=c ? p/c : (p-c)/(1-c);
                t=t*t*(3-2*t);
                float offset=p<=c ? Mathf.Lerp(-_sweepDegrees*0.5f,0,t) : Mathf.Lerp(0,_sweepDegrees*0.5f,t);
                _angle=_attackStart+Mathf.DegToRad(offset);
            }
            if (p>=1) _visualSwing=false;
        }
        else if (aim.LengthSquared()>0.01f)
            _angle=Mathf.LerpAngle(_angle,aim.Angle(),1-Mathf.Exp(-Mathf.Max(1,Config.AimSpeed)*(float)delta));
        if (_owner.Guard.Active) { _owner.Guard.Face(aim); _angle=_owner.Guard.Facing.Angle(); }
        Rotation=_angle; _toolSprite.Position=new Vector2(orbit,0); ZIndex=Mathf.Sin(_angle)<-0.15f ? -1 : 1;
        bool marker=_owner.Guard.Active && _owner.Guard.Profile.ShowDebugMarker;
        if (_marker!=marker) { _marker=marker; QueueRedraw(); }
    }
    // Optional debug guard indicator, not shield artwork; changes only when guard state changes.
    // =========================================================
    public override void _Draw()
    {
        if (!_marker) return;
        float angle=Mathf.DegToRad(_owner.Guard.Profile.HalfAngleDegrees);
        DrawArc(Vector2.Zero,22,-angle,angle,20,new Color(0.5f,0.8f,1,0.8f),2,true);
    }
    #endregion
}
