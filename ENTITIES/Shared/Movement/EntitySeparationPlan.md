# ES0–ES4 — Shared Entity Separation and Engagement Positioning

> **Status: PLANNED, documentation only (23 October 2026).** No steering, AI, scene or Resource code is implemented by this plan. **ES** means Entity Separation, distinct from AC actor collision and C3 combat feedback. Implement only the user-requested slice; update status after real validation.

## Problem and goals

In the F6 World practice setup, five skeletons can chase the same player and remain clumped together. Existing AC0–AC4 physical collision prevents body overlap but does not provide intelligent steering around other moving actors. `ENTITIES/Shared/AI/GroundChase.cs` follows static-obstacle navigation waypoints and ultimately targets player position, while each mover uses `MoveAndSlide()`. This often produces congestion even with correct colliders.

Create **one reusable entity separation foundation**, with data-driven species interpretations. Skeletons may fan out, zombies may prefer close hordes, and large trolls may seek clearance. Avoid custom per-species movement controllers. Keep **local separation** (how to avoid neighbors) separate from **engagement positioning** (where to approach or stand around target).

## Proposed ownership and files

```text
ENTITIES/Shared/
├── Movement/
│   ├── README.md
│   ├── ActorCollisionPlan.md       # AC phases complete for original scope
│   ├── Knockback.cs                # C3.2 transient reaction; preserve
│   ├── EntitySeparationPlan.md     # this roadmap
│   ├── Separation.cs               # proposed bounded steering helper
│   └── SeparationProfile.cs        # proposed Resource values
└── AI/
    ├── GroundChase.cs              # existing single authoritative enemy mover
    └── EngagementPositioning.cs    # optional ES3 targeting policy
```

File names are proposals, not requirements to add unused placeholders. Shared behavior under `Movement/`; target/engagement decisions under `AI/`. Data should connect through existing `EntityDefinition` Resources (optional `SeparationProfile` reference) and work for any future Entity species, not skeleton-only. Prefer shared profiles and defaults rather than one `.tres` per species.

## Phases

| Phase | Scope | Status |
| --- | --- | --- |
| **ES0 — Audit** | Inspect pursuit, actor/query indexing, existing collisions, C3.2 priority, streaming limits | Planned |
| **ES1 — Local separation** | Bounded nearby-actor steering blended with existing chase velocity | Planned |
| **ES2 — Species profiles** | Resource-driven distance/strength/personality with optional opt-out | Planned |
| **ES3 — Engagement positioning** | Optional distributed approach/attack positions around target | Planned |
| **ES4 — Regression and performance validation** | Multi-enemy, narrow terrain, chunk lifecycle, combat, profiling and handoff | Planned |

### ES0 — Audit current movement and available neighbors

- Read `CodePreferences.md`, root README, `ENTITIES/Shared/Entity.cs`, `EntityDefinition.cs`, `AI/GroundChase.cs`, `Movement/Knockback.cs`, `ActorCollisionPlan.md`, `AC4_Validation.md`, C3.2 validation and F6 practice actor setup.
- Inspect whether the world already has a suitable **actor spatial index / active actor registry**. Reuse it if appropriate; otherwise propose a bounded local physics query or a small active-agent grid. Avoid `GetNodesInGroup` over the whole world per enemy per frame.
- Confirm how static-obstacle paths, `StopDistance`, attack wind-up, push/stagger, death, disable/re-enable, P3 loaded-area limits and SO2 ownership interact.
- Capture a reproducible five-skeleton baseline (clumping duration, actor penetration, jitter and approximate frame-time/CPU), distinguishing physics collision from crowd intelligence.
- **Pass:** document chosen neighbor source, frequency/caps, movement priority, test fixture and minimal files to change before implementation.

### ES1 — Shared local separation (first runtime slice)

- Add a reusable **steering calculation**, not a second mover. Consume nearby active body positions/radii and produce an avoidance vector; existing `GroundChase` remains the only enemy owner of `Velocity` and `MoveAndSlide()`.
- Give higher weight to excessively close neighbors with falloff near desired spacing; blend with intended waypoint heading, preserving navigation and physical body collision.
- Use bounded local detection, a fixed maximum neighbors per agent, staggered low-rate recalculation (proposed **5–10 Hz**, tune after profiling), and cached steering between updates. Do not allocate new collections each physics tick.
- Add short-lived **side preference / directional persistence** to avoid oscillation when two actors choose how to pass. Prevent an agent's separation direction randomly flipping each frame.
- Respect stationary/passive entities (no unnecessary movement ticking), walls, trees, navigation bounds, streamed chunks and current scene ownership. Never move with raw `GlobalPosition +=` or teleports.
- Preserve attack reach/stop distance, and don't cause perpetual retreat or orbiting when the player is in reach. Separation should reduce unnatural pileups but **not guarantee tactical surrounding** (reserved for ES3).
- **Pass:** five skeletons can move around one another substantially better than baseline on open ground, while damage, pathing and normal attack opportunities remain functional.

### ES2 — Data-driven species separation profiles

- Implement optional `SeparationProfile : Resource` or equivalent compact reusable data referenced from `EntityDefinition`, with sensible code/default Resource fallback when omitted.
- Suggested independent settings: `Enabled`, `PreferredSpacing`, `SeparationStrength`, `NeighborRadius`, `MaxNeighbors`, `UpdateInterval`, `SidePersistence`, `CrowdingTolerance`. Clamp pathological values and make interpretation clear.
- Future personality presets (illustrative, not production balances):
  - **Skeleton:** moderate spacing, strong separation, optionally flank-oriented later.
  - **Zombie:** lower spacing/strength, deliberately close grouping without overlapping.
  - **Troll:** broad spacing/high clearance, larger body radius and slower adjustment.
- Do not equate separation strength with actual colliders; AC physics remains authoritative. No subclass or unique steering script per species. Passive NPCs/wildlife can opt in independently of hostile chase.
- **Pass:** two species-style disposable fixtures demonstrate different spacing tendencies using the same algorithm; no modification to existing skeleton attack stats or save schema.

### ES3 — Optional engagement positioning (separate tactical phase)

- Provide optional `EngagementPositioning` policy that chooses **different stable approach/engagement points** around a target, rather than every attacker requesting the target's exact center.
- Use approach sectors/slots or seeded offsets with stable ownership and low reassignment frequency; avoid a compulsory central combat manager. Allow actors to give up an unreachable slot and fallback to direct pursuit, respecting static obstacles.
- Profile options could include `Direct`, `Spread`, `Flank`, `Surround`; **don't promise full tactical coordination in ES1–ES2**.
- Keep real attack reach, directional attack aim and target validity independent of preferred position. Never delay all attacks merely to maintain a formation.
- **Pass:** several skeletons tend to engage from different reachable angles; a zombie-style direct approach remains possible via data, and no thrashing on target movement.

### ES4 — Validation, performance and handoff

- Test 1, 2, 5, 10+ active enemies on open ground, in corridors and around trees/rocks. Watch for permanent queues, jitter, collision tunneling, deadlocks, spinning/oscillation, attack starvation, and frame-time spikes.
- Test moving player, player stop, death, chunk streaming/unload, disable/re-enable, knockback/stagger, shield and dodge. During C3.2 push/stagger, reaction takes priority and **separation must not override it**; resume smoothly after.
- Keep actor collision on AC layers/masks intact; validate C2/C2.5 attack targeting and line of sight, tree interactions, P3/SO2 and P4 save compatibility. Transient steering state must not become persisted save state.
- Run Godot Mono/.NET build, headless import and relevant focused fixtures; record actual outcomes and distinguish known native-shutdown baseline faults. Perform Windows F6/world visual tests and, if available, CPU/GC profiling. Don't claim performance improvements without evidence.
- Update this plan and Movement README per finished phase. Codex should commit only the approved scope and leave ES3 optional until explicitly requested.

## Integration and priority rules

1. **Collision is authoritative:** no steering may move through actors, scenery or unloaded space.
2. **Knockback/stagger wins:** the existing reaction motor owns motion while active; don't mix in separation.
3. **Attack state remains authoritative:** never cancel a committed attack just because separation updated; follow current attack-state and cooldown contracts.
4. **Path navigation is primary:** separation is a modest local steering adjustment, not a replacement for obstacle navigation.
5. **Engagement selection is optional:** ES3 changes desired destination, not shared movement or damage rules.
6. **Idle work stays near zero:** no global every-frame actor scans; bound neighbor count and sampling frequency.

## Suggested Codex execution order

**Job A: ES0 + ES1 + ES2** after inspection: choose an efficient neighbor source, add shared separation steering, wire it to `GroundChase` and expose reusable species profile settings. Validate five-skeleton F6 behavior and avoid C3.2 conflicts.

**Job B: ES3** only if the user wants coordinated spacing/flanking beyond local avoidance.

**Job C: ES4** final stress tests and validation handoff. Record real test results after each job; don't label all phases complete because one fixture passes.

**Out of scope:** new skeletal attack animations, full group tactics, squad leaders, advanced navmesh crowd simulation, adding physics push by simply walking into enemies, per-species scripts, wholesale combat rebalance, and changes to AC collision-layer setup without a demonstrated reason.
