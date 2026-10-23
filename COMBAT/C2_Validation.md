# C2 shared melee implementation and validation — 22 October 2026

C2 is implemented; core combat/save checks pass. Broader regression limitations below remain open, so this is not blanket graphical/performance or full-suite acceptance. C3–C5 are not implemented by this change.

## Scope and ownership

- Shared `COMBAT/Weapons/AttackExecution` + Resource `AttackProfile` own request, wind-up, contact window, recovery, cooldown and one confirmed contact per swing. Player input and generic pursuit supply intent, not separate damage rules.
- `COMBAT/HitDetection/MeleeHitQuery` uses the current physics world for nearby body candidates, then receiving geometry, direction, factions and obstruction rays. Props keep their `IToolHitTarget` response; actors get C1 damage/results with real attacker stats.
- `ICombatTarget` describes shared receiving geometry/policy. Player and generic Entity implement it. No per-skeleton combat class, global CombatManager, always-on Area2D, extra mover or world-root lookup.
- Existing `AxeConfig` inherits `AttackProfile`, preserving serialized axe timing/reach and default 1 damage/0.38s/85% contact. Pursuing entities optionally use `EntityDefinition.MeleeAttack`; null selects the authored generic `DefaultEnemyMelee.tres`. Default enemy deals 6 damage, 36 reach, 0.9s swing + 0.35s extra cooldown. Disable melee independently of chase.
- Existing sprint/dodge, shared health bars, PNG art/grip, navigation, trees/fire and P4 schema/identity remain. No existing `.tscn`/`.tres` text changes in C2. Engine-generated script UID and PNG import sidecars are included.

Work began on `17f29b9`; concurrent upstream PNG-art commit `39844724ab29ed3a875e632bc958ab2586da8e45` was fast-forwarded before final combined validation. The subsequent Markdown-only cleanup `d315840` was also incorporated; it changes no tested runtime source. C2's commit is based on that newer documentation head. Existing save compatibility means saves made with that baseline content; C2 does not silently bypass content rejection caused by earlier independent scene/art changes. No schema or migration policy changes. Real baseline disk checkpoints were produced in a separate unmodified checkout and read in C2; no real profile storage was targeted.

## Actual validation

Linux headless Godot `4.7.2.stable.mono.official.ed1daf0bf`, .NET SDK 8.0.415/runtime 8.0.21, authored SDK/target unchanged. Restore/build succeeds without compiler warnings/errors; editor import succeeds without errors. This container intermittently fails MSBuild startup while reading process information; retry/direct MSBuild invocation succeeds without source changes.

| Check | Result |
| --- | --- |
| C2 production-component fixture | Exit 0, **46 checks**, no ERROR lines |
| C1 foundation | Exit 0, **10037 checks**, no ERROR lines |
| P4 write / separate-process read | Exit 0, **43 / 39 checks**, no ERROR lines |
| Disk checkpoint from unmodified pre-C2 PNG baseline | Exit 0, **39 checks**, no ERROR lines |
| SO2 ownership | 60 assertions pass, exit 0, but renderer/ObjectDB/RID errors at shutdown; not a clean pass |
| P3 streaming | **3587 assertions pass**, then native C# shutdown failure, exit -6; not a clean pass |
| Scenery visibility/cancellation | `Cancellation left a frontier stuck`, exit -11; same assertion also fails on unmodified baseline |
| Generic trees, production fire, local lights | All exit 0 without ERROR lines; trees/lights 0 failures, fire lifecycle checks pass |

C2 covers no idle queries, duplicate requests, wind-up/contact/recovery/cooldown, one receiving target, attacker/profile snapshots, speed scaling, defense consuming a contact, narrow windows and large time steps, wrong aim/distance, moving targets entering/leaving a window, self/friendly/excluded-neutral filtering, nearest target selection, wall obstruction, dead/queued/disabled actors, 256-candidate saturation failing closed, real Oak/Pink prop hits without actor damage, actual input-to-axe and skeleton-to-player adapters, interrupted wind-ups, passive actor dormancy and health-bar settling. It instantiates production components without a WorldScene, demonstrating no overworld-root dependency.

### Existing regression failures and investigation

Uncapped earlier runs logged `SO2 OWNERSHIP PASS` then native shutdown failures (`RenderingServer::get_singleton() is null`, dummy texture RID/ObjectDB leaks, and occasionally exit -11). P3 likewise logged its gameplay PASS before native/refcount/unsafe-reference shutdown errors. Visibility failed `Cancellation left a frontier stuck` at its fixed 260-render-frame settle boundary. The **unmodified baseline** reproduces ownership/P3 shutdown errors and the same visibility cancellation failure. These are not recorded as clean regression passes, even where the process returns 0.

An initial pre-PNG save-read run also printed all 39 assertions before exit -11; a serial rerun exits 0. An intermediate lighting run was invalidated by merging the new asset paths before importing them; refreshed combined imports/runs supersede that result. No production GC forcing, renderer patch, relaxed assertion or streamer change was added. Final combined world runs use `--max-fps 60` to give render-frame-based fixtures repeatable pacing; this is a test limiter, not an FPS measurement. Paced final runs still reproduce the ownership/P3 shutdown errors and visibility cancellation failure. These remain open; no full-suite pass is claimed.

## Reproduce

Godot **Mono** 4.7.2, .NET 8 SDK. From repo root, run `dotnet build ./isometric-fantasy-2d.csproj`, then `Godot --headless --editor --path . --import`. Replace `Godot` with your installed console executable. Inspect ERROR lines and process exit, not just PASS text.

```text
Godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
Godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
Godot --headless --max-fps 60 --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Generation/ValidateSceneryVisibility.tscn
Godot --headless --max-fps 60 --path . --script TESTS/Trees/validate_generic_trees.gd
Godot --headless --max-fps 60 --path . --script TESTS/Fire/validate_production_fire.gd
Godot --headless --max-fps 60 --path . --script TESTS/Lighting/validate_local_lights.gd
```

P4 intentionally writes only `user://p4-validation` through `CampaignSession.TestStorageRoot`. Codex additionally used isolated XDG_DATA_HOME directories. To verify prior checkpoints, build/import baseline `3984472` in a separate checkout and run write with its isolated user-data directory; then run C2 read with the same directory **without C2 write first**. Verify frozen IDs/map hashes/edits and injured/dead player state, not merely successful scene loading.

## Performance and target-PC handoff

Activation: successful player request wakes an existing tool physics callback; nearby AI submits a request through its existing pursuit callback. Shared execution/query are plain objects with no idle callback. Idle query count stays unchanged, including 1000 explicit idle Advance calls in the fixture. Contact queries run only during the configured window until one accepted target; cooldown/recovery issue none. Cached native query shapes/parameters update changed settings only. Godot contact results still allocate active query arrays/dictionaries; no allocation-free or FPS-improvement claim.

Shutdown: completion/cancellation sleeps tool attack physics; death stops movement/input/tool and enemy pursuit; process-mode suspension cancels stale wind-ups; scene exit detaches listeners/disposes native query objects. Existing continuous player aim/movement and active enemy pursuit remain. Passive actors gain no attack executor or processing. Scaling is bounded by nearby physics candidates (cap 256, fail closed on saturation), not whole loaded-world groups or distant chunks.

Manually check F5 New/Continue and F6 with the PNG actor/axe: aim/arc, dodge-away misses, successive axe hits, skeleton chase/wind-up/recovery and player HP loss, tree leaves/wobble distinct from actor damage, health-bar placement/Y-sort, walls/rocks, F7/F9, streaming exit/re-entry, save/quit/profile switching and damaged/dead restores. Profile idle, single hit, repeated swings and several enemies; confirm query counts and active callbacks settle. No rendered frame-time/GPU/GC allocation measurement was performed here. Telegraphed art, hit reactions/knockback, numbers/particles, invulnerability decisions and a death/retry loop remain later phases.
