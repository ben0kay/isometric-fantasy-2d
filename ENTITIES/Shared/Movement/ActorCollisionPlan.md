# Actor-to-actor collision — implementation plan

> **Status: AC0 source audit complete (2026-10-23); AC1 implemented, awaiting runtime validation; AC2–AC4 pending.** AC0 was documentation only; AC1 now changes layers and masks. Runtime blocking and performance acceptance remain unverified.

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

AC0 is complete as a source audit; AC1 is implemented awaiting validation; AC2–AC4 remain pending. AC means **Actor Collision** and is distinct from combat C3.

| Phase | Objective | Deliverable |
| --- | --- | --- |
| **AC0 — Audit — COMPLETE (source)** | Inventory existing physics layer/masks across actors, solids and melee hit queries | Matrix, migration and affected-file inventory below; no runtime acceptance implied |
| **AC1 — Actor blocking — implemented, validation pending** | Physically block player, enemies and future wildlife using shared feet colliders | Player and shared Entity scene layer/mask configuration |
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

## AC0 findings — 23 October 2026

**Audited revision:** `71eaeb7f0274e3c9b3925dd3b7e7136ec9a9c030`. Searched all 365 tracked `.cs`, `.tscn` and `.tres` files plus `project.godot`; inspected body definitions, scene instancing, query consumers, movement, navigation, death, streaming retirement and campaign fingerprint code. No `AGENTS.md` was present. This is static evidence, not a Godot physics test.

### Current collision matrix

Numeric values below are bitmasks: `1` = physics layer 1; `2` = layer 2; `3` = layers 1 and 2.

| Owner / consumer | Layer | Mask | Verified source behavior |
| --- | --- | --- | --- |
| `ENTITIES/Player/Player.tscn` | 1 (implicit) | 1 (implicit) | CharacterBody2D, 14×10 rectangle at (0,-5) |
| `ENTITIES/Shared/Entity.tscn` | 1 (implicit) | 1 (implicit) | CharacterBody2D, radius-9 circle at (0,-5); Entity duplicates/configures radius at readiness |
| Shared Tree, Rock01, production Torch | 1 (implicit) | 1 (implicit) | StaticBody2D; tree/rock polygons and torch rectangle; tree footprint may disable when invalid |
| `WorldScene.AddBounds` / runtime Bounds body | 1 (implicit) | 1 (implicit) | StaticBody2D with four rectangle walls |
| DEBUG player, skeleton, trees, rocks, torch and bounds | 1 (implicit) | 1 (implicit) | Independent scenes/controllers; no production migration assumed |
| Test-created static bodies | 1 (implicit) | 1 (implicit) | Includes melee wall and broad-phase saturation fixtures |
| `AttackProfile` / snapshotted `AttackValues` | — | 1 default | No authored resource override found |
| `MeleeHitQuery` overlap **and** obstruction ray | — | attack mask | Bodies only, source RID excluded; nearby candidate filtering and actor hurt geometry remain separate from feet collision |
| `CampaignSaveController.Initialize` arrival overlap | — | player mask | Excludes player RID, detects blocked saved arrival after physics synchronization |
| `StaticObstacleNavigation.Build` | — | no physics mask | Rasterizes StaticBody2D polygon/rectangle children; ignores moving actors by body type |

No explicit body layer/mask assignment, per-bit setter, scene-instance collision override, or layer-2 allocation was found. No layer names are configured in `project.godot`. No authored Area2D/RigidBody2D bodies were found. All current solid bodies share default layer 1. Therefore player/entity and entity/entity movement are **already configured to block**; AC1 must verify behavior rather than describe blocking as wholly absent. This does not prove non-overlap at spawn, under large steps, or in crowds.

### Movement and lifetime findings for AC2

- Player walking, sprint and dodge feed the same `Velocity` / `MoveAndSlide()` path. `Streaming.CanMoveTo` checks the proposed position before movement. Hop modifies sprite offset only, leaving the body/collider intact. No collision-bypassing movement teleport was found in these action paths.
- GroundChase owns the enemy's `MoveAndSlide()`. Navigation avoids static scenery, not actors; bunching is possible and belongs to AC3. GroundChase does not directly call `Streaming.CanMoveTo`; placement refresh suspends the persistent test entity outside active bounds. C3.2 must not assume that player boundary guard exists on enemies.
- Entity death cancels pursuit/attack, zeros velocity and defers collision-shape disabling; fade completion queues deletion. Player death stops movement/actions but currently leaves its collider enabled. AC2 must explicitly decide/test dead-player blocking rather than assume both actors share cleanup.
- Chunk release removes its actors subtree and queues freeing, while the authored skeleton is persistent and suspended through placement refresh. Process suspension must not be treated as evidence that every physics collider has disappeared; test disabled ownership, reactivation and same-frame retirement.
- Normal spawn/restore assigns actor position directly. This is initialization, not locomotion. New skeleton placement uses static navigation free-space checks; restored entity positions are retained. Arrival overlap uses the player's mask, so any layer change must retain detection of both scenery and actors.

### Recommended AC1 migration and affected files

Use the plan's explicit classification: **world solids layer 1, actor bodies layer 2, actor movement mask 3**. This is an authoring/query distinction, not a new blocking algorithm. Keep existing feet shapes, combat hurt geometry and single movers. Keep DEBUG independent. Do not place actors on both layers merely to hide missing query updates.

| File / area | Required follow-up |
| --- | --- |
| `ENTITIES/Player/Player.tscn`, `ENTITIES/Shared/Entity.tscn` | Explicit actor layer 2 / movement mask 3, verified in production instances |
| `COMBAT/Weapons/AttackProfile.cs` | Default contact mask 3 so attacks still see actors **and** scenery |
| `COMBAT/HitDetection/MeleeHitQuery.cs` | Preserve shared overlap/ray visibility of both layers; retain source exclusion, friendly-body obstruction, faction and tool filtering |
| `project.godot` | Name WorldSolids and Actors layers if adopting the split |
| Shared Tree/Rock/Torch scenes and runtime bounds | Explicitly document/configure world layer 1; static-body movement masks need no new actor-detection algorithm |
| `SAVE/WorldState/CampaignSaveController.cs` | Verify arrival query still catches actor and static-solid overlaps using mask 3; change only if tests expose a defect |
| Combat fixtures / new collision fixture under `TESTS/` | Audit custom masks and test actor/actor, actor/world, obstruction, trees and blocked arrival; keep synthetic static blockers on layer 1 |
| PlayerMovement, GroundChase, Entity, placement and chunk retirement | AC2 lifecycle/boundary review above; no speculative manager or extra idle loop |

Alternate minimal route: make the current layer-1 setup explicit and validate it without a split. That is sufficient for basic blocking, but gives no actor/solid query classification. The split is recommended for clear authoring; it must be delivered atomically with combat mask updates. Neither route is implemented by AC0.

### Save compatibility

`CampaignSession.BuildContentFingerprint` recursively hashes text of referenced `.tscn` and `.tres` dependencies from World.tscn and DefaultWorldConfig.tres; C# files are **not** part of that traversal. Player/Entity scene changes therefore change the fingerprint even when only making defaults explicit. The existing SO1/SO2 bridge cannot be presumed to accept changed dependency text. AC1 must record the incompatibility and use disposable F6/new test campaigns, or supply a separately justified narrowly verified migration. Never bypass RequireContent or rewrite existing campaign files. AC0 edits Markdown only and leaves the fingerprint unchanged.

### Checks performed and remaining acceptance

- Repository-wide source searches completed for physics bodies, layer/mask fields and setters, overlap/ray queries, movement calls, scene overrides, death and unload paths. Reviewed C2/C2.5 test coverage; existing combat tests do not establish the full AC actor-blocking matrix.
- No runtime behavior changed, no added callbacks/allocations, no performance claim. Godot and dotnet were not available on PATH in this environment; build, import, physics scenes, campaign read/write and graphical profiling were **not run**. They remain AC4 acceptance work, with focused runtime checks also required for AC1/AC2.
- Required next evidence: player/skeleton and skeleton/skeleton stop/slide; wall/tree/torch contact; dodge and hop; dead-player/dead-enemy policy; disabled/re-enabled ownership; unload ghost checks; contact/ray regressions; blocked saved arrival; two/five/many-actor bottlenecks. Include large-step/boundary tests before C3.2 strong displacement.

**AC0 complete:** collision matrix, migration approach and affected files documented. **AC1–AC4 and C3.2 remain unimplemented/unaccepted by this audit.**

## Execution order

Recommended order: **AC0 → AC1 → AC2 → AC4**, with **AC3 optional** when crowd issues justify it. Combat **C3.2 knockback/stagger** should follow validated AC1 and AC2 actor/obstacle collision, rather than assuming it is safe to displace actors.

**Out of scope:** new entity AI, collision-based combat damage, advanced crowd steering, pushing actors around by walking into them, jump-over-enemy physics, hit particle feedback and player-specific collider hacks.


## AC1 implementation — 23 October 2026

Built from `19d0086`. Production player/shared Entity now explicitly use layer 2 (Actors), mask 3 (WorldSolids + Actors). Shared trees, rocks, torches and runtime bounds explicitly retain layer/mask 1. `project.godot` names the two layers. `AttackProfile.CollisionMask` defaults to 3, which feeds both melee overlap and obstruction ray; no authored attack-resource overrides exist. Faction/tool filtering, hurt radii, existing feet shapes and movement motors are unchanged. Campaign arrival already uses the player's mask, so it now detects both categories without a second query implementation. DEBUG remains independent on layer 1.

`TESTS/Combat/ValidateActorCollision.tscn` adds automated production-body swept collision and arrival-style overlap checks. It uses direct swept motion only inside the disposable fixture; gameplay continues using its existing MoveAndSlide motors. Existing ValidateMeleeExecution covers attack direction/factions, walls, actor hits and Oak/Pink tool interactions with the new default mask. No new production processing, queries, timers or per-entity helpers were added: physics costs occur through existing movement/attack paths. No FPS improvement is claimed.

**Validation:** repository source/mask review and `git diff --check` passed. Godot and dotnet are absent on PATH; compilation, import and every in-engine fixture below are **not run**. AC1 is implemented, **awaiting local validation**, not accepted. AC2 lifecycle/boundaries, AC3 crowd findings and AC4 regression acceptance remain outstanding; C3.2 is deferred.

Run from project root with Godot 4.7.2 Mono/.NET 8 (`godot` below means the local Mono executable):

```sh
dotnet build isometric-fantasy-2d.csproj
godot --headless --path . --editor --import --quit
godot --headless --path . res://TESTS/Combat/ValidateActorCollision.tscn
godot --headless --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
godot --headless --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
godot --headless --path . res://TESTS/Combat/ValidatePlayerActions.tscn
godot --headless --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
```

Expect exit 0 and fixture PASS messages (AC1 expects 11 checks); record any native-shutdown failures separately using the existing C2 baseline evidence. Run separate-process P4 write/read per `SAVE/P4_Copilot_Handoff.md` using isolated test storage; verify ordinary old-fingerprint refusal preserves campaign data. Player/Entity/scenery text changes intentionally change the content fingerprint; no migration/bypass was introduced. Use F6 World or a new test campaign, not an existing campaign assumed compatible.

Manual F6 with Debug → Visible Collision Shapes: walk/sprint/dodge into skeleton/tree/rock/torch; slide around edges; verify skeleton contact and LMB/MMB/sword/enemy hits, including walls and tree chopping. Test two shared Entities blocking each other and Y sorting. Crowd navigation, death/disabled ownership and streaming cleanup require AC2–AC4 evidence before knockback. No visual test room or custom debug panel is added in this slice.
