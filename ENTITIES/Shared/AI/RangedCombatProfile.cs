// Reusable enemy ranged policy; weapon appearance and projectile contact data keep separate owners.
using Godot;
using System;
[GlobalClass]
public partial class RangedCombatProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public ProjectileProfile Projectile { get; set; }
    [Export] public EntityEquipmentDefinition Weapon { get; set; }
    [Export] public float MinimumRange { get; set; } = 24;
    [Export] public float PreferredDistance { get; set; } = 200;
    [Export] public float MaximumRange { get; set; } = 320;
    [Export] public float WindupSeconds { get; set; } = 0.75f;
    [Export] public float AimLockSeconds { get; set; } = 0.2f;
    [Export] public float CooldownSeconds { get; set; } = 1.2f;
    [Export] public Vector2 MuzzleOffset { get; set; } = new(10,0);
    [Export] public bool RequireLineOfSight { get; set; } = true;
    // Bind once per navigation/profile configuration; malformed data fails closed before query allocation.
    // =========================================================
    public RangedCombatValues Snapshot(EffectiveStats stats)
    {
        if(!GodotObject.IsInstanceValid(Projectile) || !float.IsFinite(MinimumRange) || MinimumRange<0 || MinimumRange>500
            || !float.IsFinite(MaximumRange) || MaximumRange<32 || MaximumRange>1000 || !float.IsFinite(PreferredDistance)
            || PreferredDistance<MinimumRange || PreferredDistance>MaximumRange || !float.IsFinite(WindupSeconds) || WindupSeconds<0.1f || WindupSeconds>5
            || !float.IsFinite(AimLockSeconds) || AimLockSeconds<0 || AimLockSeconds>WindupSeconds || !float.IsFinite(CooldownSeconds)
            || CooldownSeconds<0.1f || CooldownSeconds>10 || !MuzzleOffset.IsFinite() || MuzzleOffset.Length()>24) throw new ArgumentException("Invalid ranged combat profile.");
        if(Weapon!=null && (!Weapon.SpriteScale.IsFinite() || !Weapon.HandOffset.IsFinite() || !Weapon.GripOffset.IsFinite()
            || !float.IsFinite(Weapon.HoldDistance))) throw new ArgumentException("Invalid ranged equipment presentation.");
        var contact=Projectile.Snapshot(stats);
        return new(MinimumRange,PreferredDistance,MaximumRange,WindupSeconds,AimLockSeconds,CooldownSeconds,MuzzleOffset,RequireLineOfSight,Projectile,contact.Impact.Targets);
    }
}
public readonly record struct RangedCombatValues(float Minimum,float Preferred,float Maximum,float Windup,float LockSeconds,float Cooldown,Vector2 Muzzle,bool RequireLOS,ProjectileProfile Projectile,CombatFaction Targets);
