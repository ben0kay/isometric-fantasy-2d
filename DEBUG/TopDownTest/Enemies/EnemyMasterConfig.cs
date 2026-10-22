// Tunable, isolated skeleton chase/health and obstacle-grid settings for the top-down debug.
using Godot;

[GlobalClass]
public partial class EnemyMasterConfig : Resource
{
    [ExportGroup("Spawn")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Vector2 SpawnPosition { get; set; } = new(465f, 130f);

    [ExportGroup("Behaviour")]
    [Export(PropertyHint.Range, "50,600,5")] public float DetectionRadius { get; set; } = 300f;
    [Export(PropertyHint.Range, "10,250,5")] public float MoveSpeed { get; set; } = 65f;
    [Export(PropertyHint.Range, "12,70,1")] public float StopDistance { get; set; } = 26f;
    [Export(PropertyHint.Range, "0.15,2.0,0.05")] public float RepathSeconds { get; set; } = 0.45f;

    [ExportGroup("Hits")]
    [Export(PropertyHint.Range, "1,20,1")] public int MaxHealth { get; set; } = 3;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float DamageFlashSeconds { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float DeathFadeSeconds { get; set; } = 0.40f;
    [Export(PropertyHint.Range, "3,24,1")] public float HitRadius { get; set; } = 10f;

    [ExportGroup("Static obstacle navigation")]
    [Export(PropertyHint.Range, "12,48,2")] public int GridCellSize { get; set; } = 24;
    [Export(PropertyHint.Range, "5,32,1")] public float CollisionClearance { get; set; } = 17f;
    [Export(PropertyHint.Range, "1,12,1")] public int TargetSearchCells { get; set; } = 6;
}
