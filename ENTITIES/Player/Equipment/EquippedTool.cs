// Player intent and axe presentation adapter; shared AttackExecution owns timing, eligibility and contact damage.
using Godot;
public partial class EquippedTool : Node2D
{
    [Export] public AxeConfig Config { get; set; }
    public AttackExecution Attack { get; private set; }
    public bool IsAttacking => Attack?.IsSwinging==true;
    private Sprite2D _playerSprite, _toolSprite;
    private float _angle, _attackStart;
    private bool _visualSwing;
    private PlayerInput _input;
    private PlayerMovement _owner;

    #region Intent and lifecycle
    // Use centralized input and existing actor health; no idle physics/contact callback.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new AxeConfig();
        _owner=GetParent<PlayerMovement>(); Attack=new AttackExecution(_owner);
        _owner.HealthState.Changed+=OnHealthChanged;
        _playerSprite=_owner.GetNode<Sprite2D>("Sprite"); _toolSprite=GetNode<Sprite2D>("ToolSprite");
        _input=_owner.GetNode<PlayerInput>("Input"); _input.ToolUseRequested+=BeginSwing;
        ProcessPriority=5; SetPhysicsProcess(false);
    }
    // A successful request wakes physics until recovery/cooldown finishes; aim is locked for this swing.
    // =========================================================
    private void BeginSwing(Vector2 requestedAim)
    {
        Vector2 aim=requestedAim.LengthSquared()>4 ? requestedAim : Vector2.FromAngle(_angle);
        if (!Config.Enabled || !Attack.TryStart(Config,aim)) return;
        _attackStart=Attack.Aim.Angle(); _visualSwing=true; SetPhysicsProcess(true);
    }
    // Health changes cancel attacks immediately; restoration only reactivates the existing aim presentation.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    {
        if (!_owner.HealthState.IsAlive) { CancelAttack(); Hide(); }
        SetProcess(_owner.HealthState.IsAlive);
    }
    // Disabled streamed/scene owners must not resume stale wind-ups on reactivation.
    // =========================================================
    public override void _Notification(int what)
    { if (what==NotificationDisabled) CancelAttack(); }
    // Cancel pending contact and let the physics callback sleep again.
    // =========================================================
    private void CancelAttack()
    { Attack?.Cancel(); _visualSwing=false; SetPhysicsProcess(false); }
    // Detach intent/health listeners and release owned native query objects.
    // =========================================================
    public override void _ExitTree()
    {
        _owner.HealthState.Changed-=OnHealthChanged;
        if (GodotObject.IsInstanceValid(_input)) _input.ToolUseRequested-=BeginSwing;
        Attack.Dispose();
    }
    #endregion

    #region Contact and presentation
    // Only active attacks tick physics; no movement integration is introduced here.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var trace=RuntimeFrameTrace.Measure("EquippedTool.AttackPhysics");
        if (!Config.Enabled) { CancelAttack(); return; }
        Attack.Advance(delta); SetPhysicsProcess(Attack.Busy);
    }
    // Preserve continuous mouse facing and the existing full-turn smooth axe sweep.
    // =========================================================
    public override void _Process(double delta)
    {
        using var trace=RuntimeFrameTrace.Measure("EquippedTool.Process");
        Visible=Config.Enabled;
        if (!Visible) { CancelAttack(); return; }
        Position=Config.HandAnchor+_playerSprite.Position;
        Vector2 direction=_input.ReadToolAim(GlobalPosition);
        if (_visualSwing)
        {
            float progress=Attack.Progress, sweep=progress*progress*(3-2*progress);
            _angle=_attackStart+sweep*Mathf.Tau;
            if (progress>=1) { _visualSwing=false; _angle=Mathf.Wrap(_angle,-Mathf.Pi,Mathf.Pi); }
        }
        else if (direction.LengthSquared()>4)
            _angle=Mathf.LerpAngle(_angle,direction.Angle(),1-Mathf.Exp(-Mathf.Max(1,Config.AimSpeed)*(float)delta));
        Rotation=_angle; _toolSprite.Position=new Vector2(Mathf.Max(4,Config.OrbitRadius),0);
        ZIndex=Mathf.Sin(_angle)<-0.15f ? -1 : 1;
    }
    #endregion
}
