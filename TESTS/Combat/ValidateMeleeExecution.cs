// C2 repeatable production-component checks: shared attacks, AI, geometry, obstructions and lifecycle safety.
using Godot;
using System;
using System.Threading.Tasks;
using FantasyGame.World.Trees;
public partial class ValidateMeleeExecution : Node2D
{
    private int _checks;
    private PlayerMovement _player;
    private Entity _enemy;
    private AttackProfile _profile;
    private AttackExecution _attack;
    private int _contacts;
    private AttackContact _last;

    // Use isolated actors, not a campaign or WorldScene; location ownership must not affect combat.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            _player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); AddChild(_player); _player.SetPhysicsProcess(false);
            _enemy=MakeEnemy(new Vector2(30,0));
            _profile=new AttackProfile { AttackSeconds=0.4f,ContactProgress=0.25f,ContactWindow=0.25f,CooldownSeconds=0.2f,BaseDamage=2,HitReach=40,TargetFactions=CombatFaction.Hostile };
            _attack=new AttackExecution(_player); _attack.Contact+=c => { _contacts++; _last=c; };
            await Sync(); Timing(); await Geometry(); await Lifecycle(); await Capacity(); await Props(); await PlayerAdapter(); await EnemyAdapter();
            _attack.Dispose(); _profile.Dispose();
            _player.QueueFree(); _enemy.QueueFree(); await Sync();
            GD.Print($"C2 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // Make ordinary data-driven Entity instances; no species-specific scene/controller in this fixture.
    // =========================================================
    private Entity MakeEnemy(Vector2 position, bool chase=false, CombatFaction faction=CombatFaction.Hostile, AttackProfile melee=null)
    {
        var e=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        e.Configure(new EntityDefinition { MaxHealth=100,ChasePlayer=chase,Faction=faction,MeleeAttack=melee });
        e.Position=position; AddChild(e); return e;
    }
    // Wind-up, bounded contact, recovery/cooldown, stat/profile snapshots and repeat-contact protection.
    // =========================================================
    private void Timing()
    {
        for (int i=0;i<1000;i++) _attack.Advance(1.0/60);
        Check(_attack.QueryCount==0 && !_attack.Busy,"Idle queried physics");
        Check(_attack.TryStart(_profile,Vector2.Right),"Request rejected");
        Check(!_attack.TryStart(_profile,Vector2.Right),"Duplicate request accepted");
        _attack.Advance(0.05); Check(_enemy.Health==100 && _attack.QueryCount==0 && _attack.Phase==AttackPhase.Windup,"Wind-up contact");
        _attack.Advance(0.06); Check(_enemy.Health==98 && _contacts==1 && _last.Damage?.AppliedDamage==2,"Shared actor damage/result");
        int queries=_attack.QueryCount;
        _attack.Advance(0.04); _attack.Advance(0.1); Check(_enemy.Health==98 && _attack.QueryCount==queries && _attack.Phase==AttackPhase.Recovery,"Duplicate contact");
        _attack.Advance(0.17); Check(_attack.Phase==AttackPhase.Cooldown && !_attack.TryStart(_profile,Vector2.Right),"Cooldown bypass");
        _attack.Advance(1); Check(!_attack.Busy && _attack.TryStart(_profile,Vector2.Right),"Cooldown did not expire");
        _attack.Cancel();
        _player.Stats.SetModifier("damage",new(CombatStat.DamageMultiplier,Multiplier:2));
        Check(_attack.TryStart(_profile,Vector2.Right),"Scaled request rejected");
        _player.Stats.SetModifier("damage",new(CombatStat.DamageMultiplier,Multiplier:3));
        _profile.BaseDamage=50; _attack.Advance(0.12);
        Check(_enemy.Health==94,"In-flight attack data changed");
        _attack.Cancel(); _profile.BaseDamage=2; _player.Stats.RemoveModifier("damage");
        _player.Stats.SetModifier("fast",new(CombatStat.AttackSpeedMultiplier,Multiplier:2));
        _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.06);
        Check(_enemy.Health==92,"Attack speed not applied"); _attack.Cancel(); _player.Stats.RemoveModifier("fast");
        _enemy.Stats.SetModifier("armor",new(CombatStat.Defense,Flat:10));
        int before=_contacts; _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12); _attack.Advance(0.05);
        Check(_enemy.Health==92 && _contacts==before+1 && _last.Damage?.AppliedDamage==0,"Blocked hit repeated");
        _attack.Cancel(); _enemy.Stats.RemoveModifier("armor");
        _profile.ContactWindow=0; _attack.TryStart(_profile,Vector2.Right); _attack.Advance(1);
        Check(_enemy.Health==90 && !_attack.Busy,"Crossed instantaneous contact window lost"); _profile.ContactWindow=0.25f;
        Check(!_attack.TryStart(_profile,Vector2.Zero) && !_attack.TryStart(_profile,new Vector2(float.NaN,0)),"Invalid aim accepted");
        _profile.ContactWindow=1; bool rejected=false; try { _attack.TryStart(_profile,Vector2.Right); } catch(ArgumentException) { rejected=true; }
        Check(rejected && !_attack.Busy,"Invalid profile accepted"); _profile.ContactWindow=0.25f;
    }
    // Contact uses current receiving position, locked aim, nearest hurt geometry and physical obstructions.
    // =========================================================
    private async Task Geometry()
    {
        int hp=_enemy.Health;
        _attack.TryStart(_profile,Vector2.Left); _attack.Advance(0.12); Check(_enemy.Health==hp,"Opposite aim hit"); _attack.Cancel();
        _enemy.Position=new Vector2(100,0); await Sync();
        _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12); Check(_enemy.Health==hp,"Out-of-reach hit"); _attack.Cancel();
        _enemy.Position=new Vector2(30,0); await Sync(); _attack.TryStart(_profile,Vector2.Right);
        _enemy.Position=new Vector2(-30,0); await Sync(); _attack.Advance(0.12);
        Check(_enemy.Health==hp,"Tracked target behind attacker"); _attack.Cancel();
        // Enter an active window after an initial miss; still only one receiving target per swing.
        _enemy.Position=new Vector2(100,0); await Sync(); _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.11);
        _enemy.Position=new Vector2(30,0); await Sync(); _attack.Advance(0.04); _attack.Advance(0.03);
        Check(_enemy.Health==hp-2,"Moving contact window missed or repeated"); _attack.Cancel();
        var friend=MakeEnemy(new Vector2(24,0),faction:CombatFaction.Player); _enemy.Position=new Vector2(100,0); await Sync();
        _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12);
        Check(friend.Health==100 && _player.HealthState.Current==100,"Self/friendly fire"); _attack.Cancel();
        friend.QueueFree(); _enemy.Position=new Vector2(30,0); await Sync();
        var far=MakeEnemy(new Vector2(48,10)); await Sync();
        hp=_enemy.Health; _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12);
        Check(_enemy.Health==hp-2 && far.Health==100,"Nearest single target selection"); _attack.Cancel(); far.QueueFree(); await Sync();
        var wall=new StaticBody2D { Position=new Vector2(15,0) };
        var shape=new RectangleShape2D { Size=new Vector2(3,40) }; wall.AddChild(new CollisionShape2D { Shape=shape }); AddChild(wall); await Sync();
        hp=_enemy.Health; _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12);
        Check(_enemy.Health==hp,"Melee passed through wall"); _attack.Cancel(); wall.QueueFree(); await Sync(); shape.Dispose();
        var neutral=MakeEnemy(new Vector2(30,0),faction:CombatFaction.Neutral); _enemy.Position=new Vector2(100,0); await Sync();
        _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12); Check(neutral.Health==100,"Excluded faction damaged"); _attack.Cancel();
        _profile.TargetFactions|=CombatFaction.Neutral; _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12);
        Check(neutral.Health==98,"Opt-in neutral faction missed"); _attack.Cancel(); neutral.QueueFree(); await Sync();
        _enemy.Position=new Vector2(30,0); await Sync();
    }
    // Queued/removed targets, source death and disabled scene owners never produce stale contacts.
    // =========================================================
    private async Task Lifecycle()
    {
        int hp=_enemy.Health;
        _attack.TryStart(_profile,Vector2.Right); _player.HealthState.Damage(100); _attack.Advance(0.12);
        Check(!_attack.Busy && _enemy.Health==hp && !_attack.TryStart(_profile,Vector2.Right),"Dead source attacked");
        _player.HealthState.Restore(100); _player.SetPhysicsProcess(false);
        _enemy.ProcessMode=ProcessModeEnum.Disabled; _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12);
        Check(_enemy.Health==hp,"Disabled target hit"); _attack.Cancel(); _enemy.ProcessMode=ProcessModeEnum.Inherit;
        _attack.TryStart(_profile,Vector2.Right); _player.ProcessMode=ProcessModeEnum.Disabled; _attack.Advance(0.12);
        Check(!_attack.Busy && _enemy.Health==hp,"Disabled source retained contact"); _player.ProcessMode=ProcessModeEnum.Inherit;
        _enemy.Position=new Vector2(100,0); var removed=MakeEnemy(new Vector2(30,0)); await Sync(); int hits=_contacts;
        _attack.TryStart(_profile,Vector2.Right); removed.QueueFree(); _attack.Advance(0.12);
        Check(_contacts==hits,"Queued target contacted"); _attack.Cancel(); await Sync();
        _enemy.Position=new Vector2(30,0); _enemy.HealthState.Restore(0); await Sync();
        _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12); Check(_contacts==hits,"Dead target contacted"); _attack.Cancel();
        _enemy.HealthState.Restore(100); await Sync();
    }
    // Dense scenes stay bounded and fail closed instead of selecting a random truncated candidate subset.
    // =========================================================
    private async Task Capacity()
    {
        var bucket=new Node2D(); AddChild(bucket);
        using var circle=new CircleShape2D { Radius=1 };
        for(int i=0;i<260;i++)
        { var body=new StaticBody2D { Position=new Vector2(40,0) }; body.AddChild(new CollisionShape2D { Shape=circle }); bucket.AddChild(body); }
        await Sync(); int hits=_contacts;
        _attack.TryStart(_profile,Vector2.Right); _attack.Advance(0.12);
        Check(_attack.OverflowCount==1 && _contacts==hits,"Dense query exceeded its contact bound");
        _attack.Cancel(); bucket.QueueFree(); await Sync();
    }
    // Real Oak/Pink tree helpers retain wobble/leaf behavior and never receive actor damage results.
    // =========================================================
    private async Task Props()
    {
        _enemy.Position=new Vector2(1000,0); await Sync();
        foreach(string path in new[] { "res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres", "res://WORLD/Content/Vegetation/Trees/PinkBlossom/PinkBlossom.tres" })
        {
            var definition=GD.Load<TreeDefinition>(path);
            var tree=GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn").Instantiate<FantasyGame.World.Trees.Tree>();
            tree.Configure(definition,0,false); tree.Position=new Vector2(45,0); AddChild(tree); await Sync();
            var response=tree.GetNode<TreeHitResponse>("HitResponse");
            using var axe=new AxeConfig(); _attack.TryStart(axe,Vector2.Right); _attack.Advance(axe.AttackSeconds);
            Check(response.ReceivedHits==1 && _last.Damage==null,"Axe prop adapter"); _attack.Cancel();
            using var melee=new AttackProfile(); int hits=response.ReceivedHits;
            _attack.TryStart(melee,Vector2.Right); _attack.Advance(melee.AttackSeconds);
            Check(response.ReceivedHits==hits,"Enemy-style melee hit prop"); _attack.Cancel(); tree.QueueFree(); await Sync();
        }
    }
    // A real input request drives the shared executor and turns physics off after the swing.
    // =========================================================
    private async Task PlayerAdapter()
    {
        _enemy.Position=new Vector2(30,0); await Sync(); int hp=_enemy.Health;
        var input=_player.GetNode<PlayerInput>("Input"); var tool=_player.GetNode<EquippedTool>("EquippedAxe");
        Check(!tool.IsPhysicsProcessing(),"Idle tool ticks attack physics");
        input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        Check(tool.IsAttacking && tool.IsPhysicsProcessing(),"Input did not start shared attack");
        input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        await ToSignal(GetTree().CreateTimer(0.65),SceneTreeTimer.SignalName.Timeout);
        Check(_enemy.Health==hp-(int)tool.EquippedItem.PrimaryAttack.BaseDamage && !tool.Attack.Busy && !tool.IsPhysicsProcessing(),"Player adapter hit count/shutdown");
        input.EmitSignal(PlayerInput.SignalName.ToolUseRequested,Vector2.Right*100);
        _player.ProcessMode=ProcessModeEnum.Disabled;
        Check(!tool.Attack.Busy && !tool.IsPhysicsProcessing(),"Suspension retained player wind-up");
        _player.ProcessMode=ProcessModeEnum.Inherit; _enemy.Position=new Vector2(1000,0); await Sync();
    }
    // Generic pursuit asks the exact same executor to attack, then cancels on suspension/death.
    // =========================================================
    private async Task EnemyAdapter()
    {
        var definition=GD.Load<EntityDefinition>("res://ENTITIES/Species/Undead/Skeletons/Basic/SkeletonBasic.tres");
        var enemy=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        enemy.Configure(definition); enemy.Position=new Vector2(30,0); AddChild(enemy); enemy.ConfigureNavigation(_player,null);
        var chase=enemy.GetNode<GroundChase>("GroundChase");
        await ToSignal(GetTree().CreateTimer(0.55),SceneTreeTimer.SignalName.Timeout);
        var bar=_player.GetNode<SharedHealthBar>("HealthBar");
        Check(bar.Visible,"Shared player bar did not show damage");
        Check(_player.HealthState.Current==94 && chase.Attack!=null,"Production skeleton failed to damage player");
        Check(enemy.Velocity==Vector2.Zero,"Enemy swing fought movement");
        int hp=_player.HealthState.Current; await ToSignal(GetTree().CreateTimer(0.25),SceneTreeTimer.SignalName.Timeout);
        Check(_player.HealthState.Current==hp,"Enemy repeated contact during recovery");
        Check(!bar.IsProcessing(),"Health bar failed to settle");
        enemy.ProcessMode=ProcessModeEnum.Disabled; Check(!chase.Attack.Busy,"Disabled enemy kept attack");
        enemy.ProcessMode=ProcessModeEnum.Inherit; await Sync();
        enemy.TakeDamage(100); hp=_player.HealthState.Current;
        await ToSignal(GetTree().CreateTimer(0.55),SceneTreeTimer.SignalName.Timeout);
        Check(_player.HealthState.Current==hp,"Lethal enemy interruption failed");
        Check(_enemy.GetNode<GroundChase>("GroundChase").Attack==null && !_enemy.GetNode<GroundChase>("GroundChase").IsPhysicsProcessing(),"Passive actor gained combat processing");
    }
    // Physics broad phase sees teleports/new colliders after fixed-frame synchronization.
    // =========================================================
    private async Task Sync() { for(int i=0;i<3;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // Fail the process immediately on the first violated gameplay contract.
    // =========================================================
    private void Check(bool value,string message) { _checks++; if(!value) throw new InvalidOperationException(message); }
}
