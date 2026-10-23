# Skeleton Archer — R1

Uses the canonical `ENTITIES/Shared/Entity.tscn`, shared GroundChase motor and RangedCombatAI. No SkeletonArcher controller or duplicate health/collision scene. The body reuses Basic’s placeholder SVG; no duplicate body artwork is introduced.

```text
Archer/
    SkeletonArcher.tres
    ArcherRangedProfile.tres
    ArrowProjectile.tres
    Equipment/
        Bow/
            SkeletonBow.tres
            Sprites/
                skeleton_bow.svg
```

- `SkeletonArcher.tres`: 50 HP, hostile, detection600, ranged opt-in; melee disabled.
- `ArcherRangedProfile.tres`: references bow and arrow; shared defaults are min24 / preferred200 / max320px, wind-up0.75s, final aim lock0.2s, cooldown1.2s, muzzle offset10px.
- `ArrowProjectile.tres`: 240px/s, maximum700px/4s,6 damage,5px push/0.06s stagger. Contact data is an embedded shared AttackProfile, not a separate damage engine.
- `Equipment/Bow/SkeletonBow.tres`: references local artwork and stores presentation scale/hand/grip/hold settings only. This is enemy equipment, separate from player ITEMS.

Open `TESTS/Projectiles/ArcherPractice.tscn`, F6. Move to dodge released arrows, use cover, approach and attack, or equip shield with3 and hold RMB. Reset restores player/archer and removes arrows. Default production World retains five Basic melee skeletons. Mixed encounters and F9 ranged widgets remain R2.

[Validation and performance handoff](../../../../../COMBAT/Projectiles/R1_Validation.md).
