# C3.3a+b — Shared particles and tree-hit leaf migration

Implemented against `4562d2f` on 23 October 2026. Targeted numerical/lifecycle and existing tree/fire tests pass. **Windows visual equivalence and target-PC performance acceptance remain pending.** C3.3c/d material profiles/combat wiring were not implemented.

## Scope and compatibility

- `FEEDBACK/Particles` owns bounded slots, immutable burst snapshots, motion and shared primitive drawing. This is a plain helper advanced/drawn by its consumer, without its own Node/callback/physics driver. TreeHitResponse remains the consumer and retains its exact scene/canvas path.
- TreeHitConfig retains wobble/count fields and adds optional `LeafParticles`. Its null default shares one leaf profile, with no new `.tres` per tree/species. TreeDefinition continues to supply foliage colour/eligibility; live particles snapshot appearance. Burn suppression explicitly clears them.
- Default leaf geometry, canopy spawn region, horizontal/downward velocity ranges, horizontal hit bias, wind, flutter, phase/spin/stretch, fade and lifetime are retained. Root scaling/mirroring and Y sorting remain inherited from the existing canvas; spawn uses the cached original sprite transform, not its live wobble.
- Wobble restores once after its window and stops transform/shadow notifications. The existing callback then handles only active leaves and sleeps after the last redraw. Default slot count is 24, with bounded round-robin replacement and 10 leaves/hit. Disable/suspension/unload clears transient state and restores art.
- **Intentional lifecycle changes:** in-flight particle colour/settings are snapshots; disabling/suspending clears effects; no redundant shadow notifications after wobble. Last-particle expiry now sleeps immediately rather than spending an extra empty frame processing. There is no intended change to the leaf look.
- Ambient leaves, tree hit/harvest rules, fire/snow/wetness modules, physics bodies and combat remain untouched. No existing scene/resource text changed, so this migration does not alter the existing text-dependency campaign fingerprint or save schema. No compatibility bypass added.
- Pine is still staged artwork only, without a production definition/population. Acceptance covers production Oak/PinkBlossom and a disposable scaled/mirrored/no-foliage case; pine was removed as a production prerequisite in the plan.

## Actual validation

Godot 4.7.2 Mono (`ed1daf0bf`), .NET SDK 8.0.415/runtime 8.0.21, Linux headless. Matching official tools were restored into temporary storage; local Godot packages supplied NuGet dependencies. No project package/version changes. Final SDK MSBuild restore/build succeeded without compiler warnings/errors; editor import succeeded without ERROR lines. A baseline MSBuild attempt encountered the existing container process-info startup error; retry succeeded.

| Check | Observed result |
| --- | --- |
| Final SDK MSBuild restore/build | Exit 0 |
| Editor import | Exit 0, no ERROR lines |
| C3.3a+b fixture | Exit 0, **45 checks**, no ERROR lines |
| Generic trees | Exit 0, **0 failures** |
| Production fire | Exit 0, lifecycle/anchors/collision/wind/snow/shadows/reset/caps pass |
| C1 / C2 / C3.2 | Exit 0, **10037 / 46 / 41 checks** |
| C2.5 action/animation fixture | Exit 1, `Thrust rotated away from aim` |
| Unchanged `4562d2f` baseline C2.5 | Same assertion and exit 1 after a separate baseline build; not introduced by particles |
| SO2 ownership | **60 assertions pass**, native shutdown exit **-11**; not a clean pass |
| P3 streaming | **4403 assertions pass**, native shutdown exit **-6** with leaked native references; not a clean pass |
| P4 write / separate-process read | Write exit 0, **44 checks**; read **39 assertions pass**, then native shutdown exit **-11**; not a clean read pass |

The particle fixture compares seeded legacy leaf spawn/trajectory with injected wind, verifies snapshots survive Resource mutation, checks overflow/expiration/reuse/zero/default-invalid bursts, and instantiates actual Oak/PinkBlossom plus synthetic mirrored/scaled art. It checks child counts, wobble restoration, leaf-only shadow notification shutdown, burning/no-foliage eligibility, disable/suspension/reentry and final idle. No production species or per-particle nodes are created by this test's migrated adapter.

The previous melee animation pass had been statically checked but not run before these tools were restored. Its C2.5 assertion is recorded separately; this job neither relaxes it nor changes animation code. Existing world/save native shutdown problems remain unresolved; assertion PASS output is not treated as a clean full-suite pass. No timeout relaxation, forced GC or streamer workaround was added.

## Performance handoff

Wake trigger: an eligible hit activates wobble and optionally a bounded leaf burst. First leaf burst allocates the helper/array lazily; later hits reuse slots. Never-hit/zero-leaf/no-foliage trees allocate no emitter/slots. Colour/profile snapshots are value data, no per-particle Nodes/Resources or per-hit child allocation. Capacity bounds loops; wind is sampled once per active tree frame. No global manager, queries, actor scans or generic-core weather/tree references.

Wobble work stops at its own completion; leaf updates continue only while leaves live. Last expiry/clear queues a final canvas redraw, then processing sleeps. Disabled ownership/chunk retirement clears and restores without stale reactivation. Target-PC CPU/GPU/GC/FPS were **not measured**; structural savings and tested callback counts are not a measured speedup claim.

## Reproduction and manual acceptance

From the project root, with matching .NET 8 and Godot Mono (`godot` may have a different local name):

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --max-fps 60 --path . res://TESTS/Feedback/ValidateParticles.tscn
godot --headless --max-fps 60 --path . --script res://TESTS/Trees/validate_generic_trees.gd
godot --headless --max-fps 60 --path . --script res://TESTS/Fire/validate_production_fire.gd
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidatePlayerActions.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateHitReactions.tscn
godot --headless --max-fps 60 --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --max-fps 60 --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Run save fixtures with isolated Godot user data, never real profiles. Open World.tscn with F6 and compare Oak/PinkBlossom hits to the preceding build: leaf size/colour, canopy origin, fall/flutters/spin/fade, wind, repeated hits and wobble/shadow synchronization. Test burnt/no-foliage trees, pause/disable/reactivation, chunk unload/reload, root scales/mirroring and camera zoom/Y sorting. Profile untouched, active, settled and many-hit trees; confirm leaf-only frames stop sprite/shadow notifications and settled trees stop callbacks/redraws. Rendered equivalence cannot be certified by headless checks.
