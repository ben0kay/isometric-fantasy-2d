# P0–P3 checks

> **BC4 — 26 October 2026:** BC4 focused fixture: godot --headless --path . res://TESTS/Generation/ValidateLargeWorld.tscn after dotnet build. Checks default size/caps, arbitrary far-query order, schema-12 restore/size and indexed shoreline parity. Added but not run here; native tools unavailable. Earlier phase generation fixtures use retained StandardWorldGeneration.tres when they intentionally require the old size. [Current handoff](../../WORLD/Generation/Authoring/BC4_Validation.md).


Focused BC2 check: godot --headless --path . res://TESTS/Generation/ValidateClimateBiomes.tscn (after dotnet build). Covers hard affinity, Tundra/Desert presence, coverage, deterministic Resource ordering, schema-11 frozen restoration, one-biome soft fallback and impossible hard requirements. Added but not run here: Godot/.NET unavailable. The standalone visual scene is DEBUG/PreviewMap/PreviewMap.tscn.


Build the C# project in the Godot .NET editor first. From the repository root:

```sh
godot --headless --path . res://TESTS/Generation/ValidateMacroGeneration.tscn
godot --headless --path . res://TESTS/Generation/ValidateFeatureReservations.tscn
godot --headless --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --path . --script res://TESTS/Generation/measure_preview.gd
```

Use your Godot .NET executable name/path. The first command checks macro correctness/authoring and the detached manifest contract; failures exit nonzero. Four biome types in the test are temporary Resource data only, not added game content. The final command verifies unchanged production scenery with/without P1 and prints startup, scenery, sampled peak node/memory and frame-monitor statistics. Headless frame timings are diagnostic only; profile the actual renderer on target hardware.

No save files are written. The original DEBUG main scene remains unchanged. F6 World then F7 opens the optional map overview. See WORLD/Generation/World/P0_P1_Audit.md for scope and limitations.

P2 feature checks cover required-zone placement across 16 seeds, exact shape/clearance, independent RNG, priority/conflict handling, bounded preferred skips, permission/owner queries and manifest feature snapshots. They do not assert future terrain reachability or implement profile saving.

P3 runs actual World startup, reverse-order neighbour generation, seam spacing, four long-distance traversal/return checks, loaded-node bounds, fire/session removal restoration and exit cleanup. measure_preview.gd explicitly disables Streaming so it continues measuring the preserved compatibility preview.

Stability/SO1: run `godot --headless --path . res://TESTS/Generation/ValidateSceneryVisibility.tscn` for actual runtime-container checks, reversible cosmetic-culling A/B counters, partial-registration cancellation, wind/snow re-entry, long-shadow influence and compatibility preview. It reports CPU diagnostic timings, not a target-GPU frame-rate claim. The right-side F9 diagnostics support manual fixed-seed A/B comparisons.

`godot --headless --path . res://TESTS/Generation/ValidateRuntimeTrace.tscn` validates optional callback tracing with a deliberately slow nested scope and real production callbacks. It checks exclusive attribution, retained slow-frame reports, hidden-panel recording, disable/reset and ownership cleanup. It does not diagnose the user's Windows/D3D12 stall by itself.

Incremental source cache coverage in ValidateSceneryVisibility: stationary scenes must not re-register sources; cancelled/unloaded chunk caches match actual props/trees; a negative-coordinate sprite crossing spatial cells blocks rain only while registered; duplicate registration and removal leave no ghost cells. The suite now passes 167 checks.

`godot --headless --path . res://TESTS/Generation/ValidateRainSleeping.tscn` covers the production dry default, no rain GPU nodes, disabled/hidden ground+liquid pools, setter-driven restart, independent ground-only splash toggling, hard zero/overcast reset, drying-to-sleep, wind on sleeping composite materials, manual dry lightning and its return to sleep, and repeated start/stop. Passed 22 checks. The visibility fixture now checks the independent shared artwork index rather than a rain-owned cache.

`ValidateSnowSleeping.tscn` verifies snow master dormancy/reactivation, settled coverage, independent snowfall/zero rate, changed GPU inputs/canvas bounds, wind/wetness composition, chunk cleanup and automatic endpoints. Run headless with a built C# assembly; target graphical/FPS checks remain separate.

`ValidatePollenBatch.tscn` checks pollen instancing, hard off/zero/reactivation, sleeping all-off atmosphere, changed shared inputs and independent ambient effects. `BenchmarkPollen.tscn` records 180 CPU callback samples per count (35/0/100/0) after warm-up, fixed seed, other atmosphere effects off; GPU timing/FPS is deliberately not inferred from headless measurements.

`ValidateCloudWindCaching.tscn` checks cloud never-active/off/zero/wake and changed shader uploads, stable vegetation wind uploads, inactive chunk/re-entry, fresh material initialization and unload restoration. Counters verify writes rather than inferring dormancy solely from SetProcess flags.

`ValidateGradeTorchSleeping.tscn` checks event-driven grade uniforms, disabled/never-active grade, all shared preview lamps, unchanged static torch settings during flicker, off/zero-energy dormancy, deferred settings/wake and removal/unsubscribe (21 checks).

`ValidateSunCaching.tscn` verifies paused caches, independent wind/actors/hit wobble/lightning, manual/settings invalidation, exact mode and pixel error/camera lifecycle (20 checks). `BenchmarkSunShadows.tscn` compares paused, 30-second and 1440-second cycles at simulated 120Hz on a fixed seed with 367 casters; eight warm-up/240 synchronous samples per case, CPU-only, no FPS/GPU claim.

Screen-dust follow-up: ValidatePollenBatch now covers 26 checks. PollenDust is a separate CanvasLayer; camera, wind and daylight must not alter it. Dust alone sleeps the owner too; viewport resize/profile events are the only CPU updates. The old camera-local/wind/daylight expectations are superseded.

`ValidateSpatialScheduling.tscn` verifies P2-A/B static skips, conservative camera envelope/exact mode, retention, re-entry, refreshed art, spatial edge cases, nearby-only occlusion, active fades/profile edits and cleanup (32 checks). `BenchmarkSpatialScheduling.tscn` samples stationary and simulated moving callback CPU times on a fixed-seed 49-chunk scene; no GPU/FPS conclusion is inferred.

## P5a/P5b

`ValidateGeographyFirst.tscn` checks six full seeded pipelines, biome-independent water, resource ordering, required reservations, complete connected eligible coverage, soft/hard profiles, narrow/mixed coarse cells, one-biome fallback, region-budget failures, schema/hash gates and disk round-trips for schema 4–8.

Open `GeographyFirstPractice.tscn` with F6 to inspect water-first generation with only one cloned biome allowed around water. R cycles river stations; F7 shows regions and water. No canonical biome/config is edited. See [validation/target commands](../../WORLD/Generation/World/Geography/P5AB_Validation.md).

## P5c ocean and larger-world checks

`ValidateOceanGeneration.tscn` uses the normal production preset, checks four seeded 10×-area worlds, ocean/non-land coverage and empty ocean scenery, bounded macro/streaming resources, F7 panel/image fit, incompatible version rejection, deep-ocean disk Continue and genuine small pre-ocean schema-8 Continue. Run with `godot --headless --path . TESTS/Generation/ValidateOceanGeneration.tscn --fixed-fps 60`. Normal user testing is **World.tscn → F6 → F7**, not a separate practice scene. [P5c results/limits](../../WORLD/Generation/Liquids/Oceans/P5C_Validation.md).
