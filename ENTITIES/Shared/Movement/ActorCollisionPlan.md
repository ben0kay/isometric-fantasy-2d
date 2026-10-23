# Actor-to-actor collision — completed implementation

> **Status: DONE for the current agreed scope — 23 October 2026.** AC0–AC2 implementation and AC4 validation are finished; the user accepts current open-world collision behavior for now. AC3 required no crowd changes based on observed behavior; corridor/bottleneck checks remain optional follow-up. Revisit this document when normal playtesting reveals a specific problem. This does **not** mean every scenario or the full regression suite passed cleanly.

## Current behavior

- **WorldSolids — physics layer 1:** solid trees, rocks, walls, torches and other blocking scenery.
- **Actors — physics layer 2:** player and shared Entity bodies (enemies, future NPCs/wildlife). Their movement mask is **3**, detecting both layers.
- Shared melee overlap and obstruction rays default to mask **3**, preserving actor hits, tree/tool interactions and scenery obstruction. Combat factions and receiving geometry remain separate from physical feet colliders.
- Existing PlayerMovement/GroundChase remain the single authoritative movers through MoveAndSlide. No extra collision manager, constant overlap scans or new per-actor physics driver.
- Player walking/sprinting/dodging retain collision and streaming guards; hop moves artwork only.
- Player death disables its feet collider; explicit health restoration restores it. Entity death retains deferred collider removal and fade cleanup.
- Actor bodies use DisableMode.Remove: disabling an inherited owner removes its bodies from physics until reenabled. Enemy suspension cancels pending attacks, clears cached pursuit and zeros velocity.
- Moving enemies check active navigation bounds and the loaded destination chunk.
- Scene-tree ownership and Y sorting remain separate from physics layers.

## Phase closure

| Phase | Final disposition |
| --- | --- |
| **AC0 — Audit** | Done. Inventoried actor/scenery layers, movement/query consumers, lifecycle and campaign fingerprint dependencies. |
| **AC1 — Shared actor blocking** | Done. Explicit layers/masks implemented; 11 focused headless checks passed. |
| **AC2 — Movement and lifetime** | Done for current scope. Lifecycle fixes implemented; 17 focused headless checks passed; initial user open-terrain acceptance recorded. |
| **AC3 — Crowds** | Closed for current scope without additional code. User observed skeleton grouping without visible overlap or major issues in open terrain. Corridors, bottlenecks and larger-crowd profiling are deferred, not claimed tested. |
| **AC4 — Regression validation** | Done as a validation pass. Build/import and targeted collision/combat checks passed. Broader world/save-read shutdown faults remain recorded separately; no clean full-suite claim. |

No additional extensive testing is required to close this implementation plan under the user's current acceptance. Continue ordinary playtesting and reopen only the relevant issue if something behaves incorrectly.

## World practice setup

In World.tscn, select:

`WorldActorsRoot/PersistentEntitiesRoot/DebugActorsRoot`

Uncheck **Enabled** to disable test skeletons. F6 direct World places the original skeleton plus four extras near the player start, using four editable Marker2D offsets and bounded walkable/separated placement. A failed safe placement logs a warning. Existing actors can be toggled in the Remote Inspector; when disabled at startup, reopen World after enabling to populate.

F5 New Campaign/Continue retains only the original saved skeleton; the four extras are disposable F6 practice and are not saved. All use canonical Entity.tscn/SkeletonBasic.tres and one shared navigation grid. See [practice instructions](../../../DEBUG/World/README.md).

## Evidence and known limits

[AC4 validation](AC4_Validation.md) is the authoritative test record, including tested revision, commands, exit codes, save-fixture correction and shutdown diagnostics. [AC2 handoff](AC2_Validation.md) preserves implementation detail and manual scenarios.

- Build/import passed; AC1 **11**, AC2 **17**, C1 **10037**, C2 **46** and C2.5 **44** checks passed headless.
- Campaign write passed **44** checks. Read passed **39** restore assertions, then exited with a shutdown crash, including on retry.
- SO2 ownership passed **60** assertions with renderer/resource shutdown errors. P3 passed **3084** assertions before a native shutdown abort.
- Those shutdown signatures are consistent with previously documented engine faults. They remain unresolved; AC4 did not freshly reproduce an unchanged baseline or prove the faults harmless.
- No Windows frame-time, GPU, GC or large-crowd performance acceptance is claimed. Automated success does not prove rendered smoothness.
- The user's manual acceptance covers observed **open terrain**, not exhaustive death/streaming/boundary/corridor scenarios.

These limits remain visible follow-up items rather than unfinished actor-collision implementation. Investigate shutdown failures separately, or alongside a reproducible gameplay issue if evidence connects them.

## Reopen when playtesting reveals an issue

Useful triggers include bodies overlapping or passing through obstacles, invisible blocking after death/unload, stale attacks after reactivation, dodge/hop bypassing collision, excessive crowd jitter/stalls, or collision-related performance spikes.

Record the scene/seed, actor count, movement/action, obstacle layout, reproduction steps and any profiler/video evidence. Add a focused regression and the smallest shared fix; do not introduce crowd avoidance merely because actors queue behind each other.

Corridor tests can be added when enclosed terrain exists or a disposable test room becomes useful. Preserve the same shared feet-collider and movement contracts.

## Save and future-work contracts

Scene/resource text changes alter the campaign content fingerprint. Unsupported old content is refused with files preserved; no blanket migration or fingerprint bypass was added. The save fixture distinguishes schema-1 health restoration from eligibility for the exact historical SO1→SO2 layout bridge.

Combat **C3.2 knockback/stagger is separate**, not implemented by AC. Its implementation must reuse these movement/collision contracts and test displacement against solids, actors and loaded-area boundaries.

Advanced crowd steering, pushing actors by walking, flying/jumping collision policies, new population AI and collision-based damage remain outside this completed pass.
