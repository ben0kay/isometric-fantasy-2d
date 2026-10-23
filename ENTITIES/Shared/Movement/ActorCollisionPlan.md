# Actor-to-actor collision — implementation plan

> **Status: PLANNED; documentation only.** No code, collision masks or scene files have been changed by this plan.

## Intent

The player, generic enemies and future wildlife should physically block each other at their **feet colliders** rather than walk through one another. Existing terrain/prop blocking, dodge, sprint, enemy pursuit, C2 melee and P3 streaming must continue working. This is a **shared actor movement/collision** feature, not C3 visual feedback.

## Clarify terminology

- **Physics collision layer:** what a physics body belongs to; a bitmask.
- **Physics collision mask:** which layers the body tests collisions against.
- **Godot node group:** a named gameplay lookup/tag, such as `world_tool_targets`; **not** a collision layer and by itself does not block movement.
- **Combat hurt geometry:** `ICombatTarget.ClosestHitPoint` / hit radius; separate from the actor's small physical feet collider.

## Current production baseline to inspect

- `ENTITIES/Player/Player.tscn`: `CharacterBody2D` with 14×10 feet `RectangleShape2D`, no explicit collision layer/mask (Godot defaults).
- `ENTITIES/Shared/Entity.tscn`: shared `CharacterBody2D` with radius-9 feet `CircleShape2D`, also default physics masks.
- `ENTITIES/Player/Movement/PlayerMovement.cs`: authoritative `MoveAndSlide()`, dash/sprint, `Streaming.CanMoveTo` boundary.
- `ENTITIES/Shared/AI/GroundChase.cs`: shared enemy pursuit and `MoveAndSlide()`, with pathing primarily against **static** obstacles.
- `COMBAT/HitDetection/MeleeHitQuery.cs` and `COMBAT/Weapons/AttackProfile.cs`: **C2 default attack collision mask is layer 1**; a naive move of actors exclusively to layer 2 would make the broad-phase hit queries miss actors.
- Also inspect world solids, tree collision and any pooled/streamed entities or debug fixtures on current HEAD before modifying masks.

## Proposed implementation for Codex

1. Audit currently used 2D physics layers and masks throughout scenes, code and hit queries. Identify layer occupancy and any scene instancing overrides; do not assume layer 2 is unused without checking.
2. Define a small, documented shared classification: world/solid collision (currently layer 1) and actor bodies (candidate layer 2). **Safely coordinate the C2 broad-phase query and obstruction ray masks before moving actors off layer 1.** An interim approach could keep actors on both layer 1 and layer 2 with movement masks of world+actors, but avoid adopting that by default without verifying melee hits and ray line-of-sight against scenery.
3. Add explicit physics layer/mask values to the player and shared `Entity.tscn`, not a script for each species. Keep the existing feet hit shapes and independent combat receiving radius. Future species inherit the shared setup; override footprint shape only where justified.
4. Preserve `MoveAndSlide()` and the single movement motor. Do not introduce custom overlap scans, constant `Area2D` queries or an actor manager merely to prevent penetration.
5. Check enemy-to-enemy blocking and chokepoints. Static-obstacle pathfinding may produce clumping; avoid opportunistically building crowd avoidance in this small pass. If required later, design bounded local separation or opt-in avoidance separately.
6. On death, confirm the actor's solid collider stops blocking at the existing proper lifecycle point. Dodge and subsequent C3.2 knockback must respect solids, actors and P3 streaming; don't teleport through collision.
7. Preserve save compatibility and validate scene-content fingerprints before altering existing `.tscn` resources. No silent P4 schema/identity bypass.

## Acceptance tests

- Player walks/sprints and dodges into a living skeleton: stops/slides rather than passing through.
- Skeleton chasing player cannot overlap/pass through player's feet collider; two skeletons don't penetrate each other during movement.
- Player can circle enemies smoothly, including corners, trees, narrow paths and large mobs.
- Existing axe/sword attacks and skeleton melee still hit correctly, including C2 directional filtering, obstruction rays and misses.
- Tool-to-tree interactions, terrain/static-prop collision, P3 chunk boundaries and SO2 actor ownership are unchanged.
- Skeleton death clears its blocking collider; streaming/despawn does not leave ghost collisions.
- Compile Godot .NET, headless import, C1/C2/C2.5 combat fixtures and P4 save-read tests; document real results and any baseline engine shutdown failures. Verify actual gameplay feel on Windows, including large enemy groups and FPS.
- Record file changes, test results and limitations; update this plan's status only after validated implementation. One commit per implementation job.

**Out of scope:** new entity AI, collision-based combat damage, advanced crowd steering, pushing actors around by walking into them, jump-over-enemy physics, hit particle feedback and player-specific collider hacks.
