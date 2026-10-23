# C3 — Combat feedback implementation notes

> **Status: C3.2 implemented; other slices retain their individual status.** The existing C1/C2 combat framework and shared `FEEDBACK/HealthBars` already exist; health-bar visual acceptance remains open. The table below distinguishes implemented slices from later planned work.
> **Goal:** Make hits readable and satisfying without duplicating combat calculations, requiring an inventory, or adding idle costs to many streamed actors.

Read `CodePreferences.md`, root README, `COMBAT/COMBAT_IMPLEMENTATION.md`, `COMBAT/C2_Validation.md`, **C2.5 notes/results at HEAD**, `ENTITIES/Shared/Health/ActorHealth.cs`, `COMBAT/Damage/DamageResolver.cs`, `FEEDBACK/HealthBars/` and reference `ben0kay/isometric-new-2d/VISUALS/Feedback/` before coding.

| Phase | Module location | Scope | State |
| --- | --- | --- | --- |
| **C3.1** | [FEEDBACK/DamageNumbers](../FEEDBACK/DamageNumbers/README.md) | World-space numbers for **actual HP removed**; configurable single-label pop/float/fade/rapid-hit merge; independent event-driven presentation | Code added; build/visual tests pending |
| **C3.2** | `ENTITIES/Shared/Movement` + `COMBAT/Weapons` | Shared, collision-safe knockback/stagger response from confirmed attacks. Axe prod stronger than swipe; integrate with existing actor movement/chase and P3 bounds | Implemented; Windows feel acceptance pending; [validation](C3_2_Validation.md) |
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

## C3.2 — Shared knockback and stagger (Codex implementation handoff)

> **Status: IMPLEMENTED — 23 October 2026.** See [C3.2 results and tuning](C3_2_Validation.md). The following handoff is retained as the implementation contract. AC is closed for its agreed scope; optional corridor testing and known native shutdown faults remain separate. Build on C2/C2.5 attack contacts and C1 health/stats. C3.1 damage numbers are independent. Implement this slice only; C3.3–C3.5 remain later work.

### Goal and player-facing behavior

- A successful **LMB axe swipe** gives a short, light directional recoil; a successful **MMB axe prod** pushes significantly farther despite its lower damage. Sword and enemy attacks should opt into the same system using their own profiles, not new bespoke controllers.
- **Knockback** is movement displacement; **stagger** briefly restricts locomotion and, only when explicitly configured, interrupts attack wind-up/recovery. Do not make all attacks automatically interruptible.
- Start with suggested inspector-tunable targets: axe swipe ~15 px knockback and 0.12 s stagger; axe prod ~55 px and 0.30 s stagger. Distances are **tuning targets**, not guaranteed teleport distances. Resistances/collisions can reduce actual displacement.
- Skeletons should be easy to displace; heavier zombies/bosses may resist, reduce, or completely ignore push/stagger while still taking damage. Support a clean optional immunity/disable flag.

### Folder and ownership proposal

```text
COMBAT/
└── Weapons/
    └── AttackProfile.cs               ← optional authored knockback/stagger values
ENTITIES/
└── Shared/
    └── Movement/
        ├── README.md
        ├── ActorCollisionPlan.md        ← related prerequisite audit
        └── Knockback.cs                 ← proposed small shared reaction helper
ENTITIES/
├── Player/Movement/PlayerMovement.cs    ← one authoritative player motor
└── Shared/AI/GroundChase.cs             ← one authoritative enemy mover
```

`Knockback.cs` is a **pure state/helper or tiny reusable component**, not a second always-on mover; choose the smallest version that works with the existing player and enemy CharacterBody2D owners. Do not put gameplay recoil logic in `FEEDBACK/`; future FX listen to reaction/results only. Avoid adding speculative empty folders/scripts.

### Implementation contracts

1. **Inspect HEAD first.** Read `CodePreferences.md`, C2 and C2.5 validation, `AttackExecution`, `AttackProfile`, `MeleeHitQuery`, `AttackContact`, `DamageResult`, `PlayerActions`, `DirectionalGuard`, player motor, `GroundChase`, `Entity.cs` and the shared actor collision plan.
2. **One confirmed contact, one reaction.** Pass optional recoil/stagger intent from the snapshotted attack profile through C2 confirmed contact to the actual receiving actor. Don't re-query hit targets, recalculate damage or subscribe to generic HP changes to guess attacker direction. Preserve true attack direction and owner identity. Normal damage, blocked frontal hits, immune hits and prop/tree strikes must each have explicitly defined eligibility. Proposed default: physical reaction only on actual applied HP loss; a shield block with 0 applied damage produces block feedback later, not push (unless separate guard impact rules are added).
3. **Profile values:** modest bounded knockback strength/distance, stagger duration and interruption policy on `AttackProfile` or a nested small Resource only if needed. Snapshot with attack start, validate finite ranges and clamp extremes. For species, expose optional resistance modifiers/flags on shared `EntityDefinition` / shared stats without hardcoding `Skeleton` or `Zombie` names. Avoid bloating C1's generic stats without a concrete need.
4. **Single authoritative physics mover.** Recoil contributes a bounded movement vector to the owner's current `MoveAndSlide()` logic. `GroundChase` must pause/compose its pursuit while recoiling, then resume without permanently zeroing recoil velocity. The player motor must compose recoil with sprint, dodge, block and hop based on explicit priority rules; never have two nodes set `Velocity` and `MoveAndSlide()` separately in the same physics frame.
5. **Stagger:** limit duration; cancel or pause pursuit/locomotion while active. Decide whether only certain attack phases are interruptible. Preserve C2 attack cooldown and keep attacks from generating delayed contacts after legitimate interruption. Document shield-guard and dodge priorities. No full-body ragdolls, knockdown, displacement teleports or new animation assets.
6. **Collisions:** validate the [shared actor collision plan](../ENTITIES/Shared/Movement/ActorCollisionPlan.md) before strong recoil. Preserve physical walls/solid scenery, actor feet body blocking and existing P3 `Streaming.CanMoveTo`. Never bypass collision with `GlobalPosition +=`, tweening transforms or direct teleport. If collision configuration is not yet validated, complete/audit that separate prerequisite first or constrain recoil to provably safe behavior; do **not** silently rewrite collision layers and break C2 hit queries.
7. **Lifetime/performance:** only owners currently recoiling/staggered need transient state updates, integrated with physics they already run. Stop after the short window, on death/unload/disabled ownership, and avoid additional physics ticks for passive actors. Cap repeated impacts and set a clear overwrite/refresh policy; don't permit infinite stagger lock from rapid hits. No group-wide scans, unmanaged timers, extra per-enemy physics drivers or FX allocations.
8. Preserve C2.5 LMB/MMB presentation, tool-to-tree interactions, P4 health saves, SO2 ownership and current stat caches. Reactions are transient and **not persisted** unless a separate future design explicitly requires it.

### Required tests / acceptance

- Production player axe: swipe lightly shoves skeleton; prod pushes farther along the forward thrust, not simply away from mouse position. The underlying 11/5 damage values remain unchanged by C3.2.
- Different attack directions, rear hits and diagonal recoil; high resistance/immune target, zero damage, overkill and blocked guard hits all behave deterministically.
- Knockback stops at trees, walls and other actors; cannot cross unloaded chunks, leave permitted streamed area or phase through obstacles on large time steps. If actor collision is still incomplete, mark this test blocked and don't claim full acceptance.
- Skeleton pauses/reacts and resumes chase; neither existing attack execution nor player sprint/dodge/hop gets stuck. Ensure dead/unloading targets cannot move/revive and stagger never leaves queued contact.
- No repeated impulses for a single hit/contact, no unbounded stacking, finite/clamped parameters, no lingering active update after expiration.
- Test player receiving enemy knockback, multiple enemies hitting concurrently, and heavy/zombie-style resistant definition through a disposable fixture—not a new production species.
- Run Godot 4.7.2 Mono/.NET 8 build, headless import, targeted C1/C2/C2.5 fixture, P4 separate-process save read/write and SO2/P3 relevant tests; report exit codes and distinguish documented native-shutdown baseline faults from new regressions.
- Manual F6/world combat test on Windows for collision, Y-sort, enemy pathing and feel. Document tuning and headless limitations in a new C3.2 validation note. Update this plan only after testing; **one GitHub commit per implementation job**.

**Explicitly out of scope:** C3.3 particles, C3.4 block VFX, C3.5 camera shake/audio, crowd steering, ragdolls, inventory, full boss behavior, automatic combat rebalance or save-schema migration.

## C3.3–C3.5 — Later presentation work

Impacts, block feedback, camera/audio should reuse the same confirmed contact/results without introducing feedback-specific damage computations. Use caps and explicit settings defaults to keep gameplay performance stable.
