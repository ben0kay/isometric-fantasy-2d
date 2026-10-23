// Removable live combat tuning; restores shared Resource values on unload, never writes assets/saves.
using Godot;
public partial class CombatDebugPanel : VBoxContainer
{
    private readonly DebugWidgets _widgets=new();
    private MovementMasterConfig _movement;
    private CombatMasterConfig _combat, _originalCombat;
    private EngagementProfile _originalEngagement;
    private bool _originalSeparation, _originalEnabled;
    private double _refreshTimer;
    private WorldScene _world;
    private Label _status;
    // Session-only overrides use a private default profile; authored per-entity profiles keep their own values.
    // =========================================================
    public override void _Ready()
    {
        _movement=GD.Load<MovementMasterConfig>("res://ENTITIES/Shared/Movement/DefaultMovementConfig.tres");
        _combat=GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres");
        _originalCombat=(CombatMasterConfig)_combat.Duplicate();
        _originalSeparation=_movement.Enabled; _originalEnabled=_movement.EngagementEnabled; _originalEngagement=_movement.EngagementDefaults;
        _movement.EngagementDefaults=(EngagementProfile)(_originalEngagement?.Duplicate() ?? new EngagementProfile());
        var profile=_movement.EngagementDefaults;
        for(Node parent=GetParent();parent!=null;parent=parent.GetParent()) if(parent is WorldScene world) { _world=world; break; }
        AddChild(new Label { Text="Live session settings (not saved)" });
        _status=new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; AddChild(_status);
        _widgets.Toggle(this,"Enemy melee",()=>_combat.EnemyMeleeEnabled,v=>_combat.EnemyMeleeEnabled=v);
        _widgets.Toggle(this,"Knockback",()=>_combat.KnockbackEnabled,v=>_combat.KnockbackEnabled=v);
        _widgets.Toggle(this,"Stagger",()=>_combat.StaggerEnabled,v=>_combat.StaggerEnabled=v);
        _widgets.Toggle(this,"Impact particles",()=>_combat.ImpactFeedbackEnabled,v=>_combat.ImpactFeedbackEnabled=v);
        AddChild(new HSeparator());
        _widgets.Toggle(this,"Local separation",()=>_movement.Enabled,v=>_movement.Enabled=v);
        _widgets.Toggle(this,"Engagement positioning",()=>_movement.EngagementEnabled,v=>_movement.EngagementEnabled=v);
        _widgets.Toggle(this,"Spread (Off = Direct)",()=>profile.Mode==EngagementMode.Spread,v=>profile.Mode=v ? EngagementMode.Spread : EngagementMode.Direct);
        _widgets.Slider(this,"Preferred distance",18,80,1,()=>profile.PreferredDistance,v=>profile.PreferredDistance=v);
        _widgets.Slider(this,"Extra spacing",0,40,1,()=>profile.Spacing,v=>profile.Spacing=v);
        _widgets.Toggle(this,"Position persistence",()=>profile.PersistenceEnabled,v=>profile.PersistenceEnabled=v);
        _widgets.Slider(this,"Persistence seconds",0,5,0.1f,()=>profile.PersistenceSeconds,v=>profile.PersistenceSeconds=v);
        _widgets.Toggle(this,"Target movement threshold",()=>profile.MovementThresholdEnabled,v=>profile.MovementThresholdEnabled=v);
        _widgets.Slider(this,"Target movement (px)",0,100,1,()=>profile.TargetMovementThreshold,v=>profile.TargetMovementThreshold=v);
        _widgets.Slider(this,"Reconsider seconds",0.2f,2,0.05f,()=>profile.ReconsiderSeconds,v=>profile.ReconsiderSeconds=v);
        _widgets.Toggle(this,"Blocked fallback",()=>profile.BlockedFallbackEnabled,v=>profile.BlockedFallbackEnabled=v);
        _widgets.Slider(this,"Blocked timeout",0.3f,4,0.1f,()=>profile.BlockedTimeout,v=>profile.BlockedTimeout=v);
        _widgets.Toggle(this,"Limit approach detours",()=>profile.DetourLimitEnabled,v=>profile.DetourLimitEnabled=v);
        _widgets.Slider(this,"Maximum extra route (px)",0,200,5,()=>profile.MaximumDetour,v=>profile.MaximumDetour=v);
        AddChild(new Label { Text="ES3 sliders tune shared defaults.\nSpecies overrides retain their own profile.\nAttack range always wins.",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        var reset=new Button { Text="Reset combat tuning",FocusMode=Control.FocusModeEnum.None }; AddChild(reset); reset.Pressed+=Reset;
        VisibilityChanged+=()=>SetProcess(IsVisibleInTree()); SetProcess(IsVisibleInTree()); _widgets.Refresh();
    }
    // Visible widgets refresh at4Hz; hidden panels have no process callback.
    // =========================================================
    public override void _Process(double delta)
    {
        _refreshTimer+=delta; if(_refreshTimer<0.25) return; _refreshTimer=0; _widgets.Refresh();
        if(_world?.Player?.HealthState==null) return;
        int actors=0,queries=0,saturated=0,positions=0;
        foreach(Node child in _world.TestActorsRoot.GetChildren())
        {
            if(child is not Entity actor || !actor.HealthState.IsAlive) continue;
            var motor=actor.GetNode<GroundChase>("GroundChase"); actors++;
            queries+=motor.SeparationState.QueryCount; saturated+=motor.SeparationState.SaturatedSamples;
            if(motor.EngagementState.HasPosition) positions++;
        }
        _status.Text=$"Player HP {_world.Player.HealthState.Current} | practice actors {actors}\nEngagement positions {positions} | separation samples {queries}\nSaturated samples {saturated}";
    }
    // Reset all debug controls, preserving the same profile object used by widget delegates.
    // =========================================================
    private void Reset()
    {
        RestoreCombat(); _movement.Enabled=_originalSeparation; _movement.EngagementEnabled=_originalEnabled;
        var p=_movement.EngagementDefaults; var original=_originalEngagement;
        if(original!=null)
        { p.Mode=original.Mode; p.PreferredDistance=original.PreferredDistance; p.Spacing=original.Spacing; p.PersistenceEnabled=original.PersistenceEnabled;
          p.PersistenceSeconds=original.PersistenceSeconds; p.MovementThresholdEnabled=original.MovementThresholdEnabled; p.TargetMovementThreshold=original.TargetMovementThreshold;
          p.ReconsiderSeconds=original.ReconsiderSeconds; p.BlockedFallbackEnabled=original.BlockedFallbackEnabled; p.BlockedTimeout=original.BlockedTimeout;
          p.DetourLimitEnabled=original.DetourLimitEnabled; p.MaximumDetour=original.MaximumDetour; }
        _widgets.Refresh();
    }
    // =========================================================
    private void RestoreCombat()
    { _combat.EnemyMeleeEnabled=_originalCombat.EnemyMeleeEnabled; _combat.KnockbackEnabled=_originalCombat.KnockbackEnabled; _combat.StaggerEnabled=_originalCombat.StaggerEnabled; _combat.ImpactFeedbackEnabled=_originalCombat.ImpactFeedbackEnabled; }
    // =========================================================
    public override void _ExitTree()
    {
        if(_movement==null) return;
        RestoreCombat(); _movement.Enabled=_originalSeparation; _movement.EngagementEnabled=_originalEnabled; _movement.EngagementDefaults=_originalEngagement;
        _originalCombat.Dispose();
    }
}
