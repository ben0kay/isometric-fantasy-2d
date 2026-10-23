# C3 — Combat feedback implementation notes

> **Status: PLANNED.** The existing C1/C2 combat framework and shared `FEEDBACK/HealthBars` already exist; health-bar visual acceptance remains open. These C3 slices are proposed tasks for Codex, not completed work.
> **Goal:** Make hits readable and satisfying without duplicating combat calculations, requiring an inventory, or adding idle costs to many streamed actors.

Read `CodePreferences.md`, root README, `COMBAT/COMBAT_IMPLEMENTATION.md`, `COMBAT/C2_Validation.md`, **C2.5 notes/results at HEAD**, `ENTITIES/Shared/Health/ActorHealth.cs`, `COMBAT/Damage/DamageResolver.cs`, `FEEDBACK/HealthBars/` and reference `ben0kay/isometric-new-2d/VISUALS/Feedback/` before coding.

| Phase | Module location | Scope | State |
| --- | --- | --- | --- |
| **C3.1** | [FEEDBACK/DamageNumbers](../FEEDBACK/DamageNumbers/README.md) | World-space numbers for **actual HP removed**; configurable single-label pop/float/fade/rapid-hit merge; independent event-driven presentation | Folder/notes only |
| **C3.2** | `ENTITIES/Shared/Movement` + `COMBAT/Weapons` | Shared, collision-safe knockback/stagger response from confirmed attacks. Axe prod stronger than swipe; integrate with existing actor movement/chase and P3 bounds | Planned |
| **C3.3** | `FEEDBACK/HitFeedback` | Small impact bursts, adjustable flash and target material/type feedback (bones, wood, metal), optional trail; do not mix gameplay damage into FX | Planned |
| **C3.4** | `FEEDBACK/HitFeedback` + defense event contracts | Different responses to normal hit, blocked damage, guard break/parry if introduced, misses; only results from authoritative C2/C2.5 rules | Planned |
| **C3.5** | `FEEDBACK/CameraShake` + audio module | Optional subtle camera impulse and attack/impact sounds, centralized tunable defaults with strong caps | Planned |

## Common contract

1. **Damage first; visuals later.** `DamageResolver` computes damage and `ActorHealth` applies actual HP change. Presentation subscribes to event/result data. A blocked hit, an overkill, a save restoration and a heal all produce correct distinct outcomes.
2. Each FX module lives under `FEEDBACK/` (damage numbers never under COMBAT or player), with a small Resource `Settings` type and `Default*.tres` only when implementing that module. Shared movement impulse logic belongs to combat/actor movement, not feedback presentation.
3. Resource-driven inspector controls; optional per-entity overrides later without deep managers or special skeleton-only scripts.
4. No constant physics queries, always-on effects or unbounded Label/Particle creation. Processing stops when animation ends and on actor/despawn/chunk retirement. Avoid per-frame allocations and orphaned callbacks.
5. Keep existing `FEEDBACK/HealthBars` and Entity red hit flash, don't duplicate health, defense, hit confirmation or player motor. Make effects reusable for sword, axe, player, enemies, future wildlife and environmental damage.
6. Preserve player tools/tree chopping and C2.5 input/action states. Shield block visuals reflect the true defense result, never infer a successful block from a raised sprite alone.
7. Validate Godot .NET build/headless import; C1/C2 combat fixtures, player/entity scene loading, existing P4 save/read/legacy compatibility and SO2/P3 world where touched. Explicitly separate automated results from Windows visual feel/performance.
8. Each subphase should have a concrete acceptance test and its own commit/handoff; mark complete only after verification. C3.1 and C3.2 can proceed separately, but C3.4 should wait for actual defense-result contract.

## C3.1 — Immediate Codex handoff

Implement `FEEDBACK/DamageNumbers/` per its README, adapting the **older project**'s `DamageNumbers` + `DamageNumberSettings` + `DefaultDamageNumberSettings.tres` structure. Use current `ActorHealth` events to display actual applied damage. Attach only to actor visual presentation, and preserve the existing no-idle-processing, rapid-hit merge and inspector-tunable approach. **Do not implement C3.2–C3.5 as a side effect.** Record validation in a dedicated note; update status on completion.

## C3.2 — Knockback, stagger

A small shared impulse or motor-coordinated recoil solution, never an unrelated script constantly overwriting `Velocity`. It should respect physics obstacles, chase intent, world chunk restrictions, and attack-specific knockback parameters. Include tests for collisions, dead/dormant actors and force caps.

## C3.3–C3.5 — Later presentation work

Impacts, block feedback, camera/audio should reuse the same confirmed contact/results without introducing feedback-specific damage computations. Use caps and explicit settings defaults to keep gameplay performance stable.
