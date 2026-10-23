# R1 — Shared ranged AI and Skeleton Archer

Implemented 23 October 2026. R1 adds a shared ranged policy, the Archer data variant, enemy-local bow presentation and a disposable playable room. R2 mixed encounters, F9 ranged widgets, finished sprite animation and target-PC visual/performance acceptance remain planned.

## Play it

Open `TESTS/Projectiles/ArcherPractice.tscn` and press **F6**. One 50-HP archer approaches, draws its bow, locks aim for the last 0.2s and fires a dodgeable arrow. Arrow keys move/dodge; LMB/MMB use existing attacks; 3 equips shield and held RMB guards. Brown cover blocks shots. Reset restores player/archer and retires arrows; no campaign is opened or saved.

Default World still uses its original five Basic melee skeletons. The integration fixture opts into archers in memory; no existing World.tscn, Basic definition or player-item Resource was edited.

## Data ownership and settings

```text
ENTITIES/Species/Undead/Skeletons/Archer/
    SkeletonArcher.tres
    ArcherRangedProfile.tres
    ArrowProjectile.tres
    Equipment/Bow/
        SkeletonBow.tres
        Sprites/skeleton_bow.svg
```

The **bow definition lives with the Archer**, and references its local artwork. It holds scale, grip, hand offset and hold distance only. Enemy presentation code lives in Shared/Equipment; there is no player inventory/item dependency. The body sprite references Basic's placeholder artwork rather than duplicating it.

EntityDefinition.Ranged is optional. The authored archer has detection 600px, 50 HP and melee disabled. Ranged defaults: min 24 / preferred 200 / max 320px, wind-up 0.75s, lock 0.2s before release, cooldown 1.2s, muzzle 10px forward from the feet plane, LOS required. Preferred/max hysteresis avoids stop/start at a single boundary; closer than minimum holds fire rather than perpetually running away. Arrow data owns 240px/s, 700px/4s lifetime/range, 6 damage, 5px push and 0.06s stagger. Contact settings embed an ordinary AttackProfile. Projectile speed/damage are not duplicated on the bow.

Scalar ranged settings snapshot when services/profile bind; call ConfigureRanged/reload after changing those scalar settings. Wind-up/cooldown snapshot current attack-speed stats at each draw start; later stat edits do not retime that draw. Projectile contact/damage stats snapshot at release through R0. Aim updates before the lock, then retains its direction even if the player moves. Release checks that locked firing line rather than re-aiming onto the dodging target.

## Runtime, priorities and performance

- `RangedCombatAI` is plain state. GroundChase retains the only enemy velocity/MoveAndSlide owner; ranged configuration suppresses/cancels melee requests even if MeleeEnabled is true. Ranged positioning does not use melee ES3 reservations. Ordinary moving pursuit retains separation/static navigation and final loaded-area guards.
- Bind target/navigation with Entity.ConfigureNavigation and the projectile owner with Entity.ConfigureRanged. Unbound ranged actors sleep (and sleep again after event-driven recoil); explicit unbinding sleeps again and rebinding wakes them. Melee/passive entities allocate no ranged helper, bow canvas or LOS query. The optional bow canvas has no process/physics callback; the motor supplies poses and only changed draw progress requests redraw.
- One reused ray parameter object per bound ranged helper. Ordinary LOS samples are 5 Hz with instance offsets; wind-up itself does not perform repeated LOS casts, and release does a fresh check. Friendly actors and scenery obstruct shots. Blocked actors test up to 4 side candidates, at most once per second; each reachable candidate can require one path check, and the existing motor remains the only route follower. No global neighbor scan or per-frame AStar selection is added.
- A valid firing position holds through cooldown. Recoil/stagger owns movement; non-interrupting reaction pauses the draw without firing from that branch. Explicit interrupting stagger consumes the pending shot and its cooldown. Death, disabled ownership, target loss and out-of-detection clear pending intent. Same-target navigation reconfiguration cancels intent while retaining native/visual helper objects; target/service/profile replacement disposes/rebinds them.
- `CombatMasterConfig.EnemyRangedEnabled` and profile Enabled gate ranged behavior without LOS queries. Existing active chase callbacks check those switches for reactivation; this is a small remaining per-tick gate, not a claim of zero active-AI callbacks when toggled Off. F9 ranged controls remain R2. The debug panel's shared-setting restoration also preserves the new master flag on unload.
- WorldScene.GetProjectileSpawner lazily creates one budget owner under the existing actor Y-sort root only when a ranged definition is selected. Placement explicitly injects it. Default melee World allocates none. Bounds cover the finite world, while read-only delegates consult the current stream; shots are not children of individual scenery chunks and cannot force generation.
- WorldChunkStream.IsSegmentLoaded performs bounded conservative grid traversal over every crossed chunk, including corner-adjacent/edge cells. World-bound arrows and LOS use this exact segment delegate; standalone R0 callers retain sampled membership if they do not provide one. The spawner retains R0's 1024px per-step conservative limit. Off-world/unloaded membership fails closed. No streaming lifecycle/index or saved chunk format was replaced.
- Default 64-shot cap remains, with one node and cached ray per active arrow. Native query-result dictionaries, candidate paths, spawn/native objects and optional rebinds allocate. The helper/bow are reused across normal navigation refresh, but pooling/batched crowd firing was not added speculatively. No CPU/GC/FPS improvement is claimed.

## Validation actually run

Godot 4.7.2 Mono, .NET 8.0.415, Linux headless with a 60 FPS limiter:

| Check | Observed result |
| --- | --- |
| Normal MSBuild restore/build | Final build exit 0; one earlier startup attempt hit the previously seen DebugUtils/process-information failure before compilation |
| Editor import | Exit 0, no ERROR lines |
| ValidateRangedCombat | 35 checks, exit 0: real damage, exclusive ranged intent, dormant/unbound/bind/rebind behavior, helper reuse, LOS budgets, master Off/On, interrupt/recoil priority, non-interrupt pause/resume, locked-aim miss, disable/re-enable, death, moving-target wall/side-search budgets, collinear and five archers, attack-speed stats and invalid profiles |
| ValidateArcherWorld | 14 checks, exit 0: playable room/reset, real streamed World with test-only archer opt-in, shared service ownership, loaded diagonal/edge/zero-length/outside-world checks, live-shot segment rejection, debug suspension and unload |
| R0 projectiles | 75 checks, exit 0 |
| Melee / reactions | 46 /41 checks, exit 0 |
| ES1 / ES3 | 64 /42 checks, exit 0 |
| World ownership / streaming | 60 /4425 checks, exit 0 |
| P4 write/read | 44 /39 checks in separate processes, exit 0 |
| F9 HUD / C1 | 16 /10037 checks, exit 0 |
| C2.5 | Exit 1: previously recorded `Thrust rotated away from aim`; player tool animation code was not changed by R1 |

These are focused headless results, not a clean full-suite or rendered acceptance claim. Native shutdown errors did not occur in these passing runs. The practice/World fixture has the existing Camera2D physics-interpolation warning. No completed Windows playtest or 100-archer performance measurement is claimed.

## Reproduce / acceptance

Build first, then:

```
dotnet build -p:NuGetAudit=false
godot --headless --path . --editor --import --quit
godot --headless --path . --max-fps 60 res://TESTS/Projectiles/ValidateRangedCombat.tscn
godot --headless --path . --max-fps 60 res://TESTS/Projectiles/ValidateArcherWorld.tscn
```

Expect 35 R1 checks and 14 World checks, both exit 0. For manual acceptance, play ArcherPractice with F6: watch draw/lock/release, sidestep after lock, use cover, hold a facing shield, attack with LMB/MMB, kill/reset and verify arrow cleanup. Inspect bow placement, arrow elevation/readability and Y-sort from different directions. Full bow sprite animation/release polish, retreat personalities, mixed practice selection and F9 ranged tuning are R2/future work.

On Windows profile 1/5/20 active arrows and multiple ranged actors near trees/chunk boundaries: inspect native query/path allocations, shot caps, source suspension/death, frame pacing and stuck firing positions. Side search is bounded opportunistic positioning, not a guarantee of tactical flanking or navigating every forest deadlock. No save-schema changes or compatibility bypass was introduced.
