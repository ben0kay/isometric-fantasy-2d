# Skeleton Archer — planned R1 variant

> **Status: PLANNED, documentation only.** No skeleton archer sprite, Resource or runtime AI is implemented here yet.

Use canonical `ENTITIES/Shared/Entity.tscn` and `EntityDefinition`; opt into the proposed shared `ENTITIES/Shared/AI/RangedCombatAI.cs` and `RangedCombatProfile.cs`. Do **not** subclass or copy shared Entity health/movement/collision/feedback.

Proposed authored data: `SkeletonArcher.tres`, optional `ArcherRangedProfile.tres`, sprite assets only when needed. Ranged AI should choose a viable firing range, check line of sight, visibly wind up, lock aim shortly before release and shoot a simple arrow via `COMBAT/Projectiles/`. The player must be able to dodge a released arrow. Keep the existing Basic melee skeleton intact.

Read [R0–R2 implementation plan](../../../../../COMBAT/Projectiles/RANGED_IMPLEMENTATION.md) before coding. Bow item/art can live in `ITEMS/Weapons/Bows/`, preserving eventual player-bow reuse. **No bespoke SkeletonArcher.cs is intended unless a species-specific mechanic later justifies it.**
