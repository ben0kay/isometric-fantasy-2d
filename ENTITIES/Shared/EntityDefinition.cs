// Resource authored entity data shared across species; suitable for future Avalonia authoring.
using Godot;
[GlobalClass]
public partial class EntityDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public Texture2D Sprite { get; set; }
    [Export] public Vector2 SpriteOffset { get; set; } = new(0f, -24f);
    [Export] public Vector2 SpriteScale { get; set; } = Vector2.One;
    [Export(PropertyHint.Range, "3,60,1")] public float CollisionRadius { get; set; } = 9f;
    [Export(PropertyHint.Range, "3,60,1")] public float HitRadius { get; set; } = 12f;
    [ExportGroup("Ground pursuit (optional)")]
    [Export] public bool ChasePlayer { get; set; } = false;
    [ExportGroup("Detection and chase")]
    [Export(PropertyHint.Range, "50,600,5")] public float DetectionRadius { get; set; } = 300f;
    [Export(PropertyHint.Range, "10,250,5")] public float MoveSpeed { get; set; } = 65f;
    [Export(PropertyHint.Range, "12,70,1")] public float StopDistance { get; set; } = 26f;
    [Export(PropertyHint.Range, "0.15,2,0.05")] public float RepathSeconds { get; set; } = 0.45f;

    [Export] public CombatStatProfile CombatStats { get; set; }
    [ExportGroup("Melee (requires pursuit intent)")]
    [Export] public CombatFaction Faction { get; set; } = CombatFaction.Hostile;
    [Export] public bool MeleeEnabled { get; set; } = true;
    [Export] public AttackProfile MeleeAttack { get; set; }
    [ExportGroup("Health and feedback")]
    [Export(PropertyHint.Range, "1,30,1")] public int MaxHealth { get; set; } = 3;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float HitFlashSeconds { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float DeathFadeSeconds { get; set; } = 0.4f;

}
