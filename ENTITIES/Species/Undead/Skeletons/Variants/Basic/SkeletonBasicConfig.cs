// Inspector-tunable Skeleton Basic movement and health values; no debug dependencies.
using Godot;

[GlobalClass]
public partial class SkeletonBasicConfig : Resource
{
    [ExportGroup("Detection and chase")]
    [Export(PropertyHint.Range, "50,600,5")] public float DetectionRadius { get; set; } = 300f;
    [Export(PropertyHint.Range, "10,250,5")] public float MoveSpeed { get; set; } = 65f;
    [Export(PropertyHint.Range, "12,70,1")] public float StopDistance { get; set; } = 26f;
    [Export(PropertyHint.Range, "0.15,2,0.05")] public float RepathSeconds { get; set; } = 0.45f;

    [ExportGroup("Health and feedback")]
    [Export(PropertyHint.Range, "1,30,1")] public int MaxHealth { get; set; } = 3;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float HitFlashSeconds { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float DeathFadeSeconds { get; set; } = 0.4f;
}
