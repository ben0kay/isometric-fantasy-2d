# C3 — Combat feedback implementation notes

> **Status: C3.2 implemented; other slices retain their individual status.** The existing C1/C2 combat framework and shared `FEEDBACK/HealthBars` already exist; health-bar visual acceptance remains open. The table below distinguishes implemented slices from later planned work.
> **Goal:** Make hits readable and satisfying without duplicating combat calculations, requiring an inventory, or adding idle costs to many streamed actors.

Read `CodePreferences.md`, root README, `COMBAT/COMBAT_IMPLEMENTATION.md`, `COMBAT/C2_Validation.md`, **C2.5 notes/results at HEAD**, `ENTITIES/Shared/Health/ActorHealth.cs`, `COMBAT/Damage/DamageResolver.cs`, `FEEDBACK/HealthBars/` and reference `ben0kay/isometric-new-2d/VISUALS/Feedback/` before coding.

| Phase | Module location | Scope | State |
| --- | --- | --- | --- |
| **C3.1** | [FEEDBACK/DamageNumbers](../FEEDBACK/DamageNumbers/README.md) | World-space numbers for **actual HP removed**; configurable single-label pop/float/fade/rapid-hit merge; independent event-driven presentation | Code added; build/visual tests pending |
| **C3.2** | `ENTITIES/Shared/Movement` + `COMBAT/Weapons` | Shared, collision-safe knockback/stagger response from confirmed attacks. Axe prod stronger than swipe; integrate with existing actor movement/chase and P3 bounds | Implemented; Windows feel acceptance pending; [validation](C3_2_Validation.md) |
| **C3.3a–d** | `FEEDBACK/Particles` + `FEEDBACK/HitFeedback` | Shared slot-based particles → migrate tree-hit leaves → material profiles → confirmed combat impact integration | Planned; a+b together first |
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

## C3.3 — Reusable particle foundation, tree-leaf migration and hit feedback

> **Status: PLANNED — no C3.3 implementation yet.** Execute in order **C3.3a → C3.3b → C3.3c → C3.3d**. Recommend asking Codex to implement **a+b together** as one scoped job, then assess the proven shared system before authoring material-specific combat effects. Do not inadvertently rebuild existing tree leaf behaviour or implement later subphases without authorization.

### Architecture and folder ownership

```text
FEEDBACK/
├── Particles/
│   ├── README.md
│   ├── ParticleEmitter.cs         # shared, bounded particle state, animation and draw
│   ├── ParticleProfile.cs         # data-driven properties with optional spawn overrides
│   └── ParticleShapes.cs          # reusable simple 2D polygon/primitive shapes
└── HitFeedback/
    ├── README.md                  # C3.3c/d consumer of shared particles
    └── HitFeedback.cs             # proposed only when combat integration needs it
WORLD/Content/Vegetation/Trees/Shared/HitResponse/
├── TreeHitResponse.cs             # retains hit registration and trunk wobble
└── TreeHitConfig.cs               # retains tree-specific wobble and burst tuning
```

The generic engine lives under `FEEDBACK/Particles/`, **not inside `FEEDBACK/HitFeedback/`**, because tree hits, mining, footstep dust, environmental effects and future magic may all consume it. Avoid scaffolding unused scripts or creating per-species `.tres` files by default. A small `Resource` profile may be shared by many species; emitter calls can override colour, count, direction or origin without duplicating profiles.

### C3.3a — Shared particle mechanics (first)

Extract the proven bounded-slot pattern from `WORLD/Content/Vegetation/Trees/Shared/HitResponse/TreeHitResponse.cs` rather than introducing a competing GPU/CPU particle framework. Shared emitter responsibilities:

- Spawn a limited count into reusable slots; no per-particle Nodes, rigid bodies or collision queries. Cap counts per burst and total capacity, and choose predictable overflow/replacement behavior.
- Per-particle position, velocity, age/lifetime, gravity/acceleration, optional wind and sideways flutter, angular rotation/spin, size/stretch, opacity/fade and configurable directional spread.
- Render lightweight reusable shapes (leaf, angular shard, circle/droplet, spark/chip), with colour/size/profile support; keep sprite-backed particles optional and only if needed later.
- Support a reusable `ParticleProfile` plus optional per-spawn parameters (e.g. foliage colour sampled from existing tree definition; source position and hit direction). Validate/clamp nonfinite or excessive profile values.
- Idle cost: do not process or redraw while empty; bounded loops while active; clear on scene/chunk retirement; never scan all actors or the world. Prefer local lightweight emitters initially rather than an always-on global manager.
- Keep particle rendering visual-only; it must never decide damage, combat hit eligibility, HP, tree resource yield, or physical knockback.

### C3.3b — Migrate **tree-hit** leaves (together with a)

Existing reference: `TreeHitResponse.cs` uses **24 reused Leaf slots**, `LeafShape` polygon draw, `DrawColoredPolygon`, leaf ages, rotation/stretch, wind from `WorldWeather`, and sleeps after its wobble/leaves finish. `TreeHitConfig` exposes `LeavesPerHit` and wobble; `TreeDefinition.FoliageColour` and `HasFoliage` determine leaf appearance/eligibility, including burnt foliage suppression.

- Make the shared emitter animate and draw **tree-hit falling leaves** while `TreeHitResponse` continues to own trunk wobble, existing tree `IToolHitTarget`, event `VisualTransformChanged`, hit count and tree-specific eligibility.
- **Preserve exact existing default visual feel**: polygon size/shape, colour inheritance, spawn zone on the tree artwork, ~18–29 downward initial velocity, ±12 sideways drift plus hit direction, wind response, flutter/spin/stretch, 2.4–3.8 s lifetime and approximately 10 configured leaves per hit (maximum 24 reusable slots). Do not change positions unintentionally through transform or coordinate-space migration.
- Preserve `TreeDefinition.FoliageColour` and `HasFoliage`; fire/burnt trees must suppress healthy leaf bursts correctly. **Do not create one hit-colour Resource per tree species**. Keep optional embedded `TreeHitConfig` overrides, not duplicate data.
- **Do not migrate ambient falling leaves yet**: hit-triggered tree leaves and ambient weather/wind leaves are separate features; evaluate ambient reuse later only if useful.
- Do not regress wobble, snow/wetness appearance, tree shadow update notifications, hit mechanics, tool harvesting, streamed tree lifetime or existing art.
- Accept a small adapter if needed to let the tree feed its local wind and foliage settings without coupling generic `FEEDBACK/Particles` directly to `WorldWeather` or tree classes.

**C3.3a+b acceptance:** production oak/pine/blossom trees retain indistinguishable hit-leaf look and wobble, including tree-colour variants and burnt/no-foliage state; no duplicate leaves, per-hit node allocations, lingering processing, stale wind/tree references or new P3 streaming stalls. Record any intentional visual deviations before considering complete.

### C3.3c — Material-specific combat impact profiles (later)

Use the shared particle core for visual profiles such as:
- **Bone:** angular pale fragments; outward burst, short life, quick spin.
- **Flesh:** small coloured droplets; directional spray and shorter fade.
- **Stone:** heavier chips/dust; stronger downward acceleration and less drift.
- **Metal:** tiny spark lines; quick outward streak/fade.
- **Wood:** existing tree-hit leaves continue unchanged; optional splinters are distinct if desired later.

Species/objects select a shared **material impact profile**, not individual particle controllers. Use configurable colour, shapes, count, trajectory, lifetime, gravity and optional per-spawn overrides. Start with only material types the production game actually needs. Skeletons can reference a shared Bone profile; future zombies/trolls use a Flesh profile. This phase authors profiles and verifies the renderer can produce distinct visuals; do not implement attack integration until d.

### C3.3d — Confirmed-combat-hit wiring and validation (later)

- Emit impact particles only from **authoritative C2/C2.5 confirmed contact/result data**, once per valid contact, positioned at real impact/closest hit point with meaningful strike direction. Do not infer damage from HP deltas or perform duplicate physics contact queries.
- Decide and document treatment of blocked/immune/missed hits; C3.4 retains distinct guard/block and miss outcome feedback. Do not emit blood or bone shards on a miss.
- Preserve existing entity red flash and C3.1 damage numbers; no duplicate flash or extra damage/knockback. Optional subtle weapon trail is a later separately tunable presentation addition, not a prerequisite for particle foundation.
- Test player/enemy contacts, multiple skeletons, corpse unload, camera zoom/Y-sort/world-space transforms and rapid strikes; cap effects during battles and streaming.
- Run Godot Mono/.NET build, headless import, targeted tree hit/fire, C1/C2/C2.5/C3.2, P3/SO2/P4 regression suites where affected. Document observed results and known shutdown-baseline failures separately from acceptance. Perform manual Windows visual/performance checks.

### Codex handoff — first job

**Implement C3.3a + C3.3b only.** Before coding read `CodePreferences.md`, root README, `TreeHitResponse.cs`, `TreeHitConfig.cs`, `TreeDefinition.cs`, tree fire/foliage interactions, and current `FEEDBACK/` conventions. Create the shared lightweight particle engine under `FEEDBACK/Particles/` and migrate **hit-generated tree leaves only** to use it without visual or gameplay regressions. Keep wobble tree-owned; do not touch ambient leaves. Do not implement bone/flesh/stone/metal combat effects yet. Update this document and new `FEEDBACK/Particles/README.md` with actual test evidence after the implementation. Keep a single implementation job/commit and preserve P4 content-fingerprint constraints.

## C3.4–C3.5 — Later presentation work

Impacts, block feedback, camera/audio should reuse the same confirmed contact/results without introducing feedback-specific damage computations. Use caps and explicit settings defaults to keep gameplay performance stable.
