# Shared Entities

`Entity.tscn` and `Entity.cs` are the universal starting point for damageable creatures (undead, wildlife, NPCs). `EntityDefinition.cs` configures identity, sprite, collision, health, damage feedback and optional behaviours. The default optional `AI/GroundChase.cs` module runs only when `ChasePlayer` is true, so neutral animals do not chase the player merely because they use the same scene.

`Entity` implements `IToolHitTarget` for the production axe and the existing `IWorldNavigationAgent` contract for world placement; hit registration/death handling is never duplicated per species. Species/variant data lives in Resources (e.g. `ENTITIES/Species/Undead/Skeletons/Basic/SkeletonBasic.tres`), not per-species scenes. More advanced AI, ground wander, flying and other locomotion should be separate optional modules, **not hardcoded in Entity.cs**.

Player scene remains independent for now, while shared health/damage can be reused later. No DEBUG files modified.
