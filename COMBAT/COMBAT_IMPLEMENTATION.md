# Combat implementation — C0–C5 Codex plan

> **Status (2026-10-22): C0–C2 implemented; see C2 validation limitations.** Shared player/entity health, lazy effective stats and deterministic damage results are live. See [C0 audit](C0_Audit.md) and [C1 validation](C1_Validation.md). [C2](C2_Validation.md) adds shared player/enemy execution and contact validation. C3–C5 encounter polish remains planned.
> **Purpose:** Make one player-versus-skeleton encounter responsive and satisfying while establishing reusable player/enemy/wildlife combat and clean integration with future caves/interiors.
> **Authorization:** Codex should review the plan and suggest improvements before structural changes. Implement only the phase explicitly requested by the user; one commit per approved job. Never auto-implement C0–C5 as one pass.

## First read: project contracts and current baseline

Read root `README.md`, `CodePreferences.md`, `SCENES/World/scene organize.md`, `SCENES/World/SO2_Validation.md`, `SAVE/README.md`, and owning READMEs. Inspect HEAD before editing:
- `ENTITIES/Player/Input/PlayerInput.cs`: centralized mouse attack request and normalized arrow movement.
- `ENTITIES/Player/Equipment/EquippedTool.cs`, `ENTITIES/Player/Interaction/ToolHitQuery.cs`: aimed axe swing, contact-timed nearest eligible hit, `IToolHitTarget`; currently tree and entity interaction.
- `ENTITIES/Player/Movement/PlayerMovement.cs` and `PlayerMasterConfig.cs`: CharacterBody2D walk/MoveAndSlide, streaming movement guard and speed config.
- `ENTITIES/Shared/Entity.cs`, `EntityDefinition.cs`, `Entity.tscn`, `AI/GroundChase.cs`: health, 1-damage tool hit, flash/death, chaser and optional resource-driven definition.
- `SCENES/World/World.tscn`: SO2 `WorldActorsRoot/PersistentEntitiesRoot` contains player/authored skeleton, separate props/chunk ownership; shared world systems under `SystemsRoot`.
- `SAVE/WorldState/` and `SAVE/Migrations/`: P4 stores the authored test skeleton's living health/death and location; changing scene/definition/scripts can change content fingerprint and prevent Continue unless a narrowly justified compatibility bridge is designed.
- `DEBUG/TopDownTest/Weapons/README.md`: older axe and tree-hit contact tests; DEBUG stays independent.
- `FEEDBACK/README.md`, `COMBAT/*/README.md`, `ENTITIES/Player/Combat/README.md`, `ENTITIES/Player/Survival/Health/README.md`: present organizational intentions.

**Existing, not yet polished:** player can swing an axe and hit registered tree/entity targets; skeleton can chase, lose health, flash and fade on death. C1 now supplies shared actor/player HP and cached effective stats. C2 supplies shared attack execution and basic enemy attacks. Shared health bars and a sprint/dodge mini-pass also exist; preserve them. Knockback, damage numbers, impact particles and encounter polish remain future work. CombatMasterConfig remains a placeholder. Confirm live behavior and current sources before modifying.

## Architecture contract: responsibilities, not one enormous combat node

- **Shared COMBAT/** owns attack execution, attack profiles, hit shape/query, target/faction filtering, resolved damage/defense and immutable hit/damage results. Both player input and enemy AI submit attack *requests* to the same contract; neither duplicates damage rules.
- **ENTITIES/Shared/** owns actor health as **one authoritative value**, combat-relevant stats and reusable receiving/reaction/movement interfaces. Avoid parallel HP in `Player/Survival/Health` when added. Keep migration compatibility with `Entity.Health` and P4 saved test actor.
- **ENTITIES/Player/** owns input bindings, equipment presentation, player-specific attack request adapter, walking/sprint/dodge and camera. Do not put shared damage rules here.
- **ENTITIES/Shared/AI/** owns enemy attack *decisions*, telegraphs and pursuit; use the shared attack executor. Skeleton remains a resource-driven `Entity.tscn`, not a special combat implementation.
- **FEEDBACK/** owns presentation-only hit flash, particles, damage numbers, transient health bars and camera impulse; drive these from confirmed results/events, not damage-calculator dependencies on UI.
- **ITEMS/Weapons/** holds item/weapon identity and data when needed; `COMBAT/Weapons/` holds attack mechanics/profiles.
- **SCENES/World/** and future Cave/Interior scenes host actors, terrain and appropriate location-owned transient effects. Combat must not assume a `WorldScene` parent or hardcoded overworld node path. **SO3/GL/GameSession are separate work; do not require them now.**
- Prefer a small optional world-local `EffectsRoot` for standalone, pooled world-space FX only when needed; attached flashes/health bars/tool FX may stay actor-local. An `EffectsRoot` does **not** compute damage and is not a mandatory `CombatManager`.
- Avoid giant base classes, copy-pasted combat scripts per species, one manager per enemy and always-on weapon physics queries.

### Proposed logical attack pipeline

`PlayerInput or AI intent → shared attack controller (alive/cooldown/animation state) → attack wind-up → active contact window → shared hit query → target eligibility/hurtbox → attacker effective stats + attack profile → target defenses → damage result → one authoritative target health mutation → hit event → optional flash/recoil/particles/number/health bar/audio → recovery`.

- `AttackProfile` as a Godot Resource should eventually define shape, reach, angle, contact window, recovery, base damage/scaling, knockback and feedback profile references; **do not create speculative unused settings in C0**.
- **Hurtbox** describes valid receiving geometry, independent from physical movement collision. **Hitbox** describes attack geometry. For melee prefer short, contact-window physics queries or geometry tests with spatial broad-phase; `Area2D` is valid when needed but does not need to run on every idle weapon. Support directional arc/cone first, later capsule/thrust, circles, projectile collision as required.
- Distinguish attack targeting (hostile/neutral, self, friendly fire) from world tool interactions: trees keep their current `IToolHitTarget` path or a compatible shared target interface, without erroneously taking actor combat damage. Preserve existing axe tree wobble/leaf behaviour.
- One attack should not damage a target repeatedly unless the attack's explicit multi-hit rules permit it; handle source ownership and targets safely across despawn/chunk unload.
- Stat foundation: base values + ordered flat and percentage modifiers + optional multiplicative modifiers → cached effective stats; invalidate only on equipment, buffs, injuries, leveling/other input changes. Damage against target defenses is a **separate** step from attacker effective stats. Define consistent clamps, rounding, stacking order and identity of modifiers before future scaling; do not introduce a full RPG attribute catalogue immediately.
- One authoritative velocity/movement integration per actor: knockback/dodge/attack slowdown must not fight `PlayerMovement` or `GroundChase` by independently overwriting velocity.
- Future locations may load one active overworld/cave/interior, but the combat pipeline works against actors and current collision world, not a particular location class.

## C0 — Audit and design lock (documentation/review first)

**Scope:** Map existing hit interfaces, axe swing timing, tree vs skeleton behavior, AI, player scene children, P4 serialized health, SO2 destination owners, collision layers/masks, feedback and game input. Identify exact migration hazards and existing tests.

**Deliverables:** Proposed smallest contracts and class ownership; attacks from both player and AI; hurtbox/hitbox choice with cost/tradeoffs; first 3–5 exposed tuning parameters; data-driven configuration and test fixture plan. Call out whether movement polish should precede enemy attacks. Request approval **before** changing gameplay architecture.

**Pass:** Real HEAD links/paths documented; no unnecessary global combat manager, no duplicated health or speculative empty components; risks and rollback described.

## C1 — Shared health, effective stats and damage result

**Scope:** Small shared attack/damage data contract, shared health source and minimal cached effective stats (e.g. damage, defense, attack speed, movement speed) used by player/skeleton. Expose deterministic resolver with explicit modifier ordering; avoid expensive recalculation per frame.

**Must preserve:** Existing `Entity.TakeDamage`, `ReceiveHit`, save/restore health/death and old saves; trees and fire damage semantics; no separate player survival HP. Introduce adapters first if migrating `Entity` outright is risky. Player receiving damage can be added without adding hunger/stamina/injury survival gameplay.

**Pass:** Unit/scene fixture proves damage, no negative HP, alive/dead transitions, defense and modifier ordering, P4 save/reload including prior checkpoints, no idle work increase.

## C2 — Shared attack execution and contact validation — implemented

**Scope:** Player input and skeleton AI request attacks through the same executor; Resource-driven axe/basic-skeleton melee profile, wind-up/contact/recovery/cooldown; direction, reach and eligible hurtbox query. Do not poll entire scene every frame. Protect against duplicate contacts, dead/unloaded targets, friendly fire/self hits.

**Must preserve:** Player axe visual and tree hits; currently authored skeleton chase/navigation; existing collision masks and streamer; one active attack result for normal basic melee. Attack requests must not depend on cave or world root node paths.

**Pass:** Player hits skeleton, skeleton hits player, misses/distance/direction blocked as expected, one target once per swing, no hit when dead/on cooldown; stream and trees regressions pass. Enemy telegraph art can be deferred to C5.

**Implementation:** Shared plain `AttackExecution`, `AttackProfile` (inherited by existing AxeConfig), contact-window physics broad phase, receiving geometry/faction/obstruction policy and generic pursuit intent. No scene changes in C2. See [results, baseline comparisons and manual handoff](C2_Validation.md); broader headless shutdown failures are not represented as clean passes.

## C2.5 — Player actions and starter equipment — implemented, visual acceptance pending

See [C2.5 Codex implementation handoff](C2_5_PlayerActions.md). Adds primary LMB sweep, alternate MMB prod, starter axe/sword switch, shield-equipped RMB block vs no-shield RMB visual jump, and data-driven attack profiles. No inventory. C2/shared damage and existing P4 scene/resource fingerprints are preserved; see [C2.5 results and outstanding checks](C2_5_Validation.md). C3 should use the resulting attack/contact/block outcomes for feedback.

## C3 — Combat feedback and perceived impact

**Codex execution plan:** [C3 Implementation Notes](C3_Implementation.md) breaks this phase into C3.1 damage numbers, C3.2 knockback/stagger, C3.3 particles/hit reactions, C3.4 block outcomes, and C3.5 camera/audio. **C3.1 source folder/requirements:** [FEEDBACK/DamageNumbers](../FEEDBACK/DamageNumbers/README.md) (scaffold only; no runtime code).

**Early health-bar slice (headless integration checked; visual acceptance pending):** Shared `FEEDBACK/HealthBars` bars have been added to player and generic entity using C1 `ActorHealth`, adapting the older project's event-driven bar design. Do not duplicate this in C3; validate and polish it with knockback, particles and floating damage numbers. No actual defense/armor meter was added.

**Scope:** Resource-tunable enemy hit flash, bounded physical recoil/knockback, small impact particles, floating damage text, conditional temporary health bar; optional audio/camera impulse with settings OFF. Separation: `DamageResult` produces presentation events, presentation never changes resolved HP.

**Performance:** Sleep when dormant, cap active objects, pool only where spawn frequency warrants, cleanly unregister on death/unload/scene change, minimize material allocations/copies and no permanent per-actor idle loop. Avoid adding many new `CanvasLayer`s.

**Pass:** One axe-vs-skeleton hit visibly reads; old tree hit remains distinct; effect count/time bounded after repeated strikes and unload; particles/numbers obey world-to-screen transforms, Y-sort and correct Z ordering.

## C4 — Movement and attack feel

**Pre-C4 movement mini-pass:** Right Shift sprint and double-tap right Shift held-arrow-direction dodge have been added to `PlayerInput` + `PlayerMovement` with Resource tuning (see Player Movement README). Build/manual acceptance still pending; C4 must **refine and reuse**, not reimplement a second sprint/dodge controller. No sprite animation, stamina or invulnerability is included.

**Scope:** Tune acceleration/deceleration, aim/facing, existing sprint/dodge with its own cooldown/possible invulnerability decision, attack movement penalties, hit-stun/knockback blending, control buffering if useful. Use `PlayerMovement` as the existing authoritative CharacterBody2D mover; common impulses where enemy chase also moves. Defer full survival stamina unless the user explicitly authorizes integration.

**Pass:** No diagonal speed exploit, no movement through unready chunks or obstacles, no permanently locked input, predictable dash/collision and balanced hit timing; baseline exploration speed and camera usability retained.

## C5 — One complete skeleton encounter and tuning

**Scope:** Refine the C2 skeleton attack/request/contact/recovery with readable telegraphs, fair player damage, defensible dodge response, death/respawn/retry in a disposable combat test; tune attack profile Resources (damage, hit window, speed, recoil, feedback). Prefer a dedicated `TESTS/Combat/` repeatable scene/fixtures reusing production components rather than introducing enemies/weapon content breadth.

**Pass:** Repeatable player-vs-skeleton loop, clear readable telegraphs, hits and misses, no sticky chasing/velocity; test a real gameplay encounter at target renderer, multiple attempts and performance with several enemies. No A-Life, loot/progression, caves or new world generator in this phase.

## Testing and non-regression — every implementation phase

1. Read `CodePreferences.md`; use deliberate method headers, Resource-driven settings, concise ownership READMEs and small commits. Ensure invalid/disabled states stop processing and listeners unsubscribe; no LINQ/scene-tree-wide per-frame scans in hot paths.
2. Run `dotnet build` with the authored Godot SDK, Godot .NET headless import and relevant in-engine combat fixtures. Run existing `TESTS/Generation/ValidateChunkStreaming.tscn`, `TESTS/SceneOrganization/ValidateWorldOwnership.tscn`, `TESTS/Save/ValidateCampaignSaves.tscn` write/read when schema/scene dependencies change, plus tree/fire/lighting tests when touched. Record exact commands and exit codes; don't claim graphical/performance acceptance from headless tests.
3. Test F5 Boot → New/Continue, F6 direct World, F7/F9 panels, tool strikes against Oak/Pink trees, skeleton spawn/chase/death, Y sorting, chunk load/unload and profile separation. Do not silently overwrite or reset `user://` campaign data.
4. Profile idle (no attacks/active FX), a single hit, multiple enemies and location exit; compare CPU/GPU frame time, allocations and active effect counts. Static effects and distant AI should sleep.
5. Update `COMBAT/README.md`, owning `ENTITIES/`, `FEEDBACK/`, `TESTS/` READMEs and this status **only after verified results**. Every implementation phase gets its own commit and concise handoff; manual play-feel feedback is required before calling C3–C5 polished.

## Codex design review — explicitly invited improvements

- Is the existing `IToolHitTarget` sufficient as a compatibility adapter or should actors/props use a narrower shared `IHittable`/`IDamageable` contract? Show nonbreaking migration and P4 risks.
- Prefer physics overlap queries, geometry tests or short-lived `Area2D` for top-down arc attacks? Compare correctness with moving targets, broad-phase cost and accurate contact timing. Do not require running idle detection.
- Could the current `Entity` health and red flash be isolated without breaking the existing save snapshot and visual behaviour? Name the canonical health owner.
- Should health and stat configuration be generic components or properties of `EntityDefinition`? Avoid one permanent runtime object per numerical stat.
- How will player and AI attacks use exactly the same request-execute-resolve flow with different adapters and optional faction filtering?
- How should movement impulses avoid conflicts with `GroundChase` and player MoveAndSlide? Is a shared impulse accumulator/physics motor necessary at C3, or can simpler solutions suffice?
- What feedback belongs attached to actors vs a location-local `EffectsRoot`? How to pool/limit FX while retaining correct transforms/Y-sort?
- Which C1–C5 steps should be split or reordered to maximize early **feel** testing and minimize save/schema churn? Recommend improvements before structural changes.

**Out of scope for initial combat foundation:** GameSession/GL0–GL3, caves, P5 world features, SO3 registry unless proven necessary, complex attributes/status stacking, full inventory/loot, magic/projectiles, PvP and combat-wide always-on global managers.
