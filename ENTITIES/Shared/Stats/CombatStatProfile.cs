// Optional authored base stats; runtime modifiers never mutate this shared Resource.
using Godot;
[GlobalClass]
public partial class CombatStatProfile : Resource
{
    private float _damage = 1, _defense, _attackSpeed = 1, _movementSpeed = 1;
    [ExportGroup("Damage and defense")]
    [Export] public float DamageMultiplier { get => _damage; set { if (_damage == value) return; _damage=value; EmitChanged(); } }
    [Export] public float Defense { get => _defense; set { if (_defense == value) return; _defense=value; EmitChanged(); } }
    [ExportGroup("Speed multipliers")]
    [Export] public float AttackSpeedMultiplier { get => _attackSpeed; set { if (_attackSpeed == value) return; _attackSpeed=value; EmitChanged(); } }
    [Export] public float MovementSpeedMultiplier { get => _movementSpeed; set { if (_movementSpeed == value) return; _movementSpeed=value; EmitChanged(); } }
    public EffectiveStats Values => EffectiveStats.Validate(new(_damage,_defense,_attackSpeed,_movementSpeed));
}
