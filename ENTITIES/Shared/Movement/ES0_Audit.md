# ES0 — Entity separation audit and ES1 proposal

**Source audit complete — 23 October 2026, revision `ece2597`.** ES1–ES4 remain unimplemented. This pass changed documentation only; its disposable baseline probe ran outside the working checkout. No production scripts/scenes/Resources, collision rules, saves or movement were changed.

## Findings

| Area | Current source behavior | Consequence for ES1 |
| --- | --- | --- |
| `Shared/AI/GroundChase.cs` | Sole enemy Velocity/MoveAndSlide owner. Static path waypoints, then target centre near the last waypoint; repath defaults to 0.45s | Blend steering into its final pursuit intent; never add another mover |
| `Shared/Navigation/StaticObstacleNavigation.cs` | AStarGrid2D rasterizes StaticBody2D polygons/rectangles, padded clearance, diagonal corner restrictions; no moving bodies | Preserve routes; separation handles moving neighbors, not pathfinding replacement |
| Player/shared Entity scenes | Actors layer **2** (mask bit value 2), movement mask **3**; entity feet circle radius 9 by default, player 14×10 rectangle, both offset (0,-5) | Query only actor layer 2; keep all AC layers/masks/shapes unchanged |
| `WorldEntityPlacement._actors` | Small authored/practice-actor list, used for initial safe spacing and navigation refresh; no general actor population registry/player entry | Do not couple shared steering to this World/DEBUG placement owner or scan this list per agent |
| `SpatialBoundsIndex<T>` | Generic bounds buckets currently owned by cosmetics/visibility; explicit registration/update/remove, result HashSet, no neighbor-count limit | Not a live actor service; adopting it needs new lifecycle/movement registration. No reason to add that machinery before measurement |
| `SceneryArtworkIndex` | Cached mostly static artwork; explicitly excludes CharacterBody2D | Unsuitable for entity neighbor positions |
| Physics broad phase | Actor bodies already registered/moved/removed by Godot; melee already uses local `IntersectShape` queries | Reuse the engine's current spatial data for ES1's small local sample |
| Lifecycle | Entity death zeros motion, stops chase and defers feet disabling; queue-free ends fade. Disabled ownership removes bodies via DisableMode.Remove. GroundChase clears attacks/path on disable | Filter health/process validity as well as physics presence; clear steering on death/suspension/reconfiguration and dispose query objects on exit |
| P3 | GroundChase checks active navigation bounds and `player.Streaming.IsPositionLoaded(next)` before its single MoveAndSlide | Apply these guards to the **final combined** velocity, not just original pursuit |
| SO2 | Persistent player/entities under existing Y-sorted roots; F6 practice descendants under DebugActorsRoot | No reparenting/new world manager; physics query is restricted to the actor's own World2D |

Source searches covered ENTITIES, CORE, WORLD runtime/generation and the practice setup. No live-actor spatial index or general active-agent registry was found. `IToolHitTarget` groups/legacy ToolHitQuery are not a neighbor index; production combat uses MeleeHitQuery. No global group scan should be introduced for ES1.

### Movement priorities verified

1. GroundChase advances C3.2 transient reaction first. While staggered or carrying recoil, that branch moves and returns; separation must never query/steer there.
2. `Reaction.NeedsUpdate` includes the short immunity/recovery window; **do not freeze movement for that entire flag**. Existing code resumes normal pursuit once no actual stagger/recoil remains.
3. Invalid/dead/disabled owners or targets cancel attacks and zero motion.
4. Existing melee enable/Advance and attack initiation run before pursuit. Committed swings freeze normal locomotion; separation must not cancel them or inject movement/contact.
5. Navigation missing, outside detection, within StopDistance or with no path: current motor stops. ES1 should not wake stationary agents merely to spread them out.
6. Only after those gates, the motor derives waypoint heading and stat-adjusted speed. This is the proposed steering insertion point, before bounds/loaded-chunk guards and MoveAndSlide.

Default pursuit is 65px/s, 300px detection, 26px StopDistance, 0.45s repath. Default melee reach is 36px against the player's hurt radius; actual contact eligibility remains with combat. Steering must diminish near a valid attack opportunity rather than repel entities from the player or starve attacks during cooldown.

`WorldEntityPlacement.RefreshNavigation` updates practice actor process modes according to local navigation bounds and preserves a disabled ancestor. Its list is not a future population solution. Chunk publication/retirement already controls inherited process/physics lifetime; separation should consume that lifecycle, not duplicate it.

## Five-skeleton baseline actually run

A disposable headless GDScript probe used the unmodified production World/compiled assembly in a detached `ece2597` checkout. Godot 4.7.2 Mono, fixed seed **213876**, F6-style direct World, default marker layout and all five real skeletons. Only probe overrides: player MaxHealth=10000 and ReactionImmune=true, to keep a stationary target alive and prevent received knockback from confounding approach distances. Attacks, enemy stats, scenery, paths, collision and steering remained unchanged. No campaign was opened/saved.

Sampled **600 physics frames / 10 simulated seconds**, with headless `--max-fps 60`. Result process exited **0**.

| Observation | Result |
| --- | --- |
| Actor count | 5 |
| Target distances after 2s | 91.2, 120.8, 132.6, 165.9, 158.5px |
| Target distances after 5s | 25.2, 40.6, 25.3, 43.9, 36.0px |
| Target distances after 10s | 25.2, 40.7, 25.3, 25.1, 25.8px |
| Minimum pair centre distance over the run | **18.00077px**, consistent with touching radius-9 feet circles, no sampled body penetration |
| Close pairs in the final 5s | **834 / 3000 pair-frame samples (27.8%)** below 24px centre distance |
| Far stalled agents after 5s (>60px from target) | No eligible samples; cannot infer corridor/queue behavior from this |
| Engine physics-process monitor samples | Mean **3.175ms**, highest reported value **14.026ms**, whole-world headless monitor readings |

Interpretation: the five actors can approach and obtain attack-range positions on this open-terrain seed. They pack tightly, but there is no demonstrated permanent approach deadlock here. This supports modest local steering, not a wholesale navigation rewrite. It does not prove all-five simultaneous contact or tactical surrounding.

The engine monitor can retain values between updates; those readings are **not independent per-step CPU samples**, isolated GroundChase timings, whole-frame latency or a Windows FPS benchmark. No steering speedup is measured. Jitter/visual feel, corridor behavior, larger groups and target-PC allocation/CPU profiling remain unmeasured. Prior user open-terrain observation also reported grouping without visible overlap/major issues.

Reproduce: open World directly, fix the world seed to 213876 in a disposable config, keep the normal five DebugActorsRoot spawns, prevent player death/recoil for the crowd-only comparison, hold player stationary, and sample pair/target distances for 600 physics steps. Original offsets are (0,-210) plus NW(-180,-170), NE(180,-170), W(-260,80), E(260,80); placement may snap/search for safe points. Use matching Godot Mono headlessly with a 60 FPS limiter, or record an equivalent Windows F6 run and profiler capture. Do not alter real authored configs/campaigns for measurement.

## Chosen ES1 neighbor source and budgets (proposal, not implemented)

Use one reusable CircleShape2D/query-parameter pair per **enabled pursuing** helper, allocated lazily. Query `DirectSpaceState.IntersectShape` against mask **2**, bodies only, excluding source and chase-target RIDs. This lets the engine maintain moving-actor spatial membership. Target exclusion prevents separation pushing enemies away from their intended player target; player movement itself remains untouched.

Starting tuning targets, to be adjusted against the same baseline:

- **5 Hz** samples per moving agent (0.20s), with instance-derived phase offsets to avoid synchronized batches; reuse cached steering between samples.
- **64px** neighborhood; choose at most **8 nearest eligible entities** using fixed bounded scratch storage and deterministic within-session tie handling. Use actual physical body radius/scale when computing clearance, not combat hurt radius. Cache needed collider geometry rather than searching the scene tree per neighbor.
- A **33-result sentinel cap** for 32 raw candidates: if saturated, count it diagnostically and skip/decay that sample rather than treat an arbitrary broad-phase subset as the nearest crowd. Physical collision still blocks. Revisit dense-crowd query/index policy in measured stress testing.
- Default desired centre spacing approximately **28px** for radius-9 bodies (physical radius sum plus soft clearance); allow bigger bodies their real clearance. Avoid forcing a large fixed ring around the player.
- Cap steering to roughly **35% of intended pursuit speed**, clamp final speed to the normal stat-adjusted cap, and preserve forward progress. Strongest influence from very close bodies, fading smoothly to zero near desired spacing.
- Keep side preference for roughly **0.4s**; use a stable fallback for perfectly coincident/collinear bodies rather than random per-frame direction flips. No engagement slots/flank selection.

These are **budgets/tuning proposals**, not measured optimal settings. Five continuously moving agents imply at most about **25 samples/s** (10 → 50/s; 100 → 500/s), fewer when attacks/reactions/stops suppress sampling. Godot's C# IntersectShape returns native/managed result collections: **do not claim zero-allocation queries** merely because shapes/parameters/scratch arrays are reused. Measure allocation/time and saturation. If this source becomes expensive, introduce a world-scoped moving-agent grid through a separately justified change, not an always-running actor registry speculatively.

Living stationary/passive Entity bodies should influence nearby movers without acquiring their own new callbacks. Filter self/target, freed/queued/dead/disabled bodies and incompatible bodies from samples. Drop cached steering after a short bounded age and clear it on reaction, target/navigation changes, death/suspension/unload; reacquire when pursuit resumes. Store vectors/IDs rather than retaining arbitrary neighbor Node references between samples.

## ES1 implementation I recommend

1. Add small `Movement/Separation.cs`: bounded sampling/state/calculation, cached steering and side persistence; **no Node or MoveAndSlide**.
2. Add the minimal shared settings Resource (`SeparationProfile.cs` plus optional EntityDefinition reference) from the outset, rather than hardcoding a skeleton behavior. Use one shared fallback for active chase; broader presets/personality tuning remain ES2. No per-species script or required per-species `.tres`.
3. Wire GroundChase only at its final pursuit-heading stage, respecting all priority/early-return gates above. Keep attacks, StopDistance and static routes primary. If wall-adjacent steering produces prolonged reduced progress, decay/reject the adjustment and retry the route rather than repeatedly forcing a blocked side.
4. Reuse the final navigation/loaded-chunk checks and physical MoveAndSlide. Local collision sliding is not a guarantee against new deadlocks; test obstacles/corners before calling it accepted.
5. Clear/dispose helper state through existing GroundChase lifecycle paths. Entity may need a small cached physical-footprint accessor for accurately scaled radii/centres; do not duplicate health or add world-wide registration.
6. Add one disposable separation fixture under TESTS: baseline vs enabled with 1/2/5/10 movers, a synthetic wall/bottleneck, passive neighbor, stable coincident start, chase target excluded, attacks still occur, recoil/stagger priority and resume, death/disable/re-enable/unload, query saturation/caps and disabled/no-neighbor idle behavior. Reuse existing AC/C2/C3.2/P3/SO2/P4 checks where touched.

ES1 should improve approach spacing; it must **not** promise evenly distributed positions around a stopped target. Stationary/in-reach attackers remain allowed to hold their positions. Explicit surrounding/engagement-point selection is ES3, and species-specific presets are ES2. The proposal to introduce minimal settings with ES1 is structural preparation, not authorization to implement all ES2/ES3 work.

Minimal prospective files: Separation.cs, SeparationProfile.cs, EntityDefinition.cs, GroundChase.cs, optionally a cached-footprint accessor in Entity.cs, focused TESTS files and owning documentation. No existing scene hierarchy/mask changes or save fields are required. If later wiring changes existing `.tres`/`.tscn` dependency text, record fingerprint consequences; never bypass campaign compatibility checks. Steering/side preference remains transient and unsaved.

**ES0 closed as source audit plus limited headless baseline. ES1 remains a proposal awaiting the user's implementation request.** Windows crowd feel/CPU/GC acceptance and extensive ES4 stress cases remain future evidence, not silently claimed complete.
