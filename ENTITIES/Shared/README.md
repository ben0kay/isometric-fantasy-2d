# Shared Entities

`Entity.tscn` and `Entity.cs` are the universal starting point for damageable creatures (undead, wildlife, NPCs). `EntityDefinition.cs` configures identity, sprite, collision, health, damage feedback and optional behaviours. The default optional `AI/GroundChase.cs` module runs only when `ChasePlayer` is true, so neutral animals do not chase the player merely because they use the same scene.

`Entity` implements `IToolHitTarget` for the production axe and the existing `IWorldNavigationAgent` contract for world placement; hit registration/death handling is never duplicated per species. Species/variant data lives in Resources (e.g. `ENTITIES/Species/Undead/Skeletons/Basic/SkeletonBasic.tres`), not per-species scenes. More advanced AI, ground wander, flying and other locomotion should be separate optional modules, **not hardcoded in Entity.cs**.

Player scene stays independent while both actors implement `ICombatant`, owning one `ActorHealth` and one lazy `ActorStats`. `Entity.Health` is a read-only compatibility view. Optional `CombatStatProfile` supplies shared authored values; modifiers remain per actor. Health/stat objects introduce no frame callbacks. See [health](Health/README.md), [stats](Stats/README.md) and [C1 validation](../../COMBAT/C1_Validation.md). No DEBUG files modified.

C2 adds `ICombatTarget` geometry/faction and optional melee intent to existing GroundChase. `EntityDefinition.MeleeAttack` overrides the generic profile, `MeleeEnabled` toggles attacks and `Faction` controls receiving eligibility. Chase remains the sole mover; no per-species combat class, new scene children or idle contact query. [C2 validation](../../COMBAT/C2_Validation.md) records evidence and limits.
