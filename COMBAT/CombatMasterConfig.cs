// Shared live combat feature switches; DEBUG controls may edit these in memory without saving assets.
using Godot;
[GlobalClass]
public partial class CombatMasterConfig : Resource
{
    [Export] public bool EnemyMeleeEnabled { get; set; } = true;
    [Export] public bool KnockbackEnabled { get; set; } = true;
    [Export] public bool StaggerEnabled { get; set; } = true;
    [Export] public bool ImpactFeedbackEnabled { get; set; } = true;
}
