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

## Implementation phases (AC0–AC4)

All five phases are planned, not implemented. AC means **Actor Collision** and is distinct from combat C3.

| Phase | Objective | Deliverable |
| --- | --- | --- |
| **AC0 — Audit** | Inventory existing physics layer/masks across actors, solids and melee hit queries | Verified collision matrix and compatibility notes |
| **AC1 — Actor blocking** | Physically block player, enemies and future wildlife using shared feet colliders | Player and shared Entity scene layer/mask configuration |
| **AC2 — Lifecycle integration** | Ensure death, dodge, sprint, streaming and despawn work with collision | No ghost colliders or movement bypass |
| **AC3 — Crowd checks** | Evaluate groups of pursuing actors and narrow pathways | Performance and clumping findings; optional small follow-up only |
| **AC4 — Validation** | Verify combat, save and physics regression tests | Recorded test results and completion status |

### AC0 — Collision layer audit

- Inspect player, shared Entity, scenery, trees, other physics bodies, and tests at current HEAD.
- Map current layer bits and movement/query masks, including C2 `MeleeHitQuery`, `AttackProfile` and obstruction rays. Existing melee targeting uses layer 1: moving actors exclusively to another layer without updating combat detection will break attacks.
- Verify layer 2 is unused before assigning it to actors. Propose world solids on layer 1 and actors on layer 2, with appropriate movement masks; preserve combat/prop interactions.
- Review P4 saved-content fingerprints if scene modifications are required.
- **Complete when:** the collision matrix, migration approach and affected files are documented.

### AC1 — Shared physical actor collision

- Configure explicit layer/mask settings for player and shared `Entity.tscn`, not separate species scripts; both must collide with world solids and living actors.
- Retain player rectangle and configurable Entity circular feet shapes, and `MoveAndSlide()` in existing authoritative movement controllers.
- Keep combat hurt geometry separate from the feet collider. Maintain melee broad-phase/obstruction compatibility and tool-to-tree hits.
- Avoid continuous overlap searches, per-actor managers or unnecessary extra physics nodes.
- **Complete when:** player/skeleton and skeleton/skeleton blocking works without disabling existing attacks or prop collisions.

### AC2 — Movement, death and streamed lifetime

- Confirm player walking, sprinting, double-Shift directional dodge and visual hop respect solid actors and scenery.
- Disable blocking appropriately when a target dies and fades; avoid ghost collisions after chunk unload, despawn or scene changes.
- Preserve P3 streamed boundaries and SO2 scene ownership. No save-compatibility bypass.
- **Complete when:** death and retirement leave no solid corpse ghosts, and no actor movement phases through other actors or walls.

### AC3 — Crowds and narrow passages (optional polish)

- Test two, five and many enemies approaching a player, around rocks and through bottlenecks.
- Expect some bunching with existing static-obstacle navigation; investigate excessive jitter, stalls and physics spikes.
- Only propose a separate, bounded and opt-in separation/avoidance enhancement if actual testing shows it is needed. No wholesale AI rewrite.
- **Complete when:** behavior, performance and any intentional limits are recorded; no body penetration or runaway costs.

### AC4 — Regression validation and handoff

- Compile Godot Mono/.NET 8, headless import and relevant C1/C2/C2.5 fixtures, P3/SO2 tests, P4 save/read and collision scenarios.
- Test real world scene player and multiple enemy collisions manually; inspect dodge, death, streaming and performance.
- Separate known engine shutdown problems from actual new regressions and record what could not be tested.
- Update individual phase statuses only after evidence; make one implementation commit per requested job.

## Dependencies

Recommended order: **AC0 → AC1 → AC2 → AC4**, with **AC3 optional** when crowd issues justify it. Combat **C3.2 knockback/stagger** should follow validated AC1 and AC2 actor/obstacle collision, rather than assuming it is safe to displace actors.

**Out of scope:** new entity AI, collision-based combat damage, advanced crowd steering, pushing actors around by walking into them, jump-over-enemy physics, hit particle feedback and player-specific collider hacks.
