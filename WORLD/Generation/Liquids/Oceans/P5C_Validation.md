# P5c ocean portion and 10× world area — 25 October 2026

Implemented; Windows Godot 4.7.2 visual/performance acceptance pending. Volcanic geography remains a separately planned P5c follow-up. This delivers the ocean portion discussed, not volcano gameplay/elevation, drainage networks, new shore artwork or swimming.

## Result

Normal World/New Campaign enables one required ocean, three lakes and one river. Bounds are 142304×106728 (10.00021× original area; about 3.16× per axis). The macro grid is 278×209 at cell size 512, 58102 cells versus 23232 previously. Chunk size, active/preload/unload radii and local scenery densities stay unchanged. F7 uses a responsive viewport panel and KeepAspectCentered whole-map texture; it shows ocean separately from land biomes and includes dimensions.

Ocean polygons are created before fresh water and biomes, with bounded seeded slots, required/optional policy, 128–1024 samples and reservation/pond/separation checks. A constrained center zone avoids wasting attempts outside finite bounds. Ocean classification uses clipped coarse cell centers; those cells store region -1 and receive no land biome/site. Partial coast cells may retain land IDs, while exact water geometry excludes scenery and supplies wading/nav/fields. All remaining land is assigned; disconnected land components use the existing bounded compatible-region fallback. Ground maps supply a neutral underwater floor without creating a fake ocean biome. Existing lake/river geography and all rendering/streaming remain shared.

Blueprint schema 9 / geography v2 / macro v3 supports ocean non-land. WaterPlan v4 hashes explicit IsOcean flags, retaining v1–v3 byte/hash layouts for older plans. Old macro versions cannot restore negative region IDs. Schema 4–8 retain frozen sizes/maps/water; no campaign is enlarged or given oceans on Continue. The exact shipped expanded preset is source-hash gated into the existing pre-water-config fingerprint reconstruction, preserving strict checks on unrelated edits and composing with other content compatibility paths.

## Checks

Linux Godot 4.5.1 Mono / .NET 8.0.21. Direct .NET 8.0.415 Roslyn compilation with Godot source generators passed without errors/warnings. `dotnet build --no-restore` aborted before compilation with the runner's System.Diagnostics.Process metadata exception (exit 134); clean target 4.7.2 build remains pending. Editor imports prepared assets but printed runner SDK/MSBuild tooling diagnostics, so they are not claimed as clean target editor validation.

| Suite | Result |
| --- | --- |
| ValidateOceanGeneration --fixed-fps 60 | **464877 checks**, exit 0: four real production seeds/repeats; 10× area and bounded grid; land/ocean lookup/coverage; reservations; required failure/optional warning; water-version rejection; actual World F7 full image/panel fit; deep-ocean/coast/return streaming; empty deep-ocean scenery; bounded tile ownership; disk deep-ocean Continue with current creation bounds/water changed; real small pre-ocean schema-8 Continue preserving bounds/hash and no new water |
| ValidateGeographyFirst | **141772 checks**, exit 0: freshwater eligibility/connected coverage, version/fingerprint gates and old schema round-trips |
| ValidateStreamedRiver --fixed-fps 60 | **3392 checks**, exit 0: real ocean-enabled production config and river fields/traversal/save/legacy Continue |
| ValidateStreamedWater --fixed-fps 60 | **136 checks**, exit 0: explicit lake-only fixture, shared fields/seams/nav/lifecycle and legacy/new Continue; count varies with seeded field/tile locations on enlarged bounds |
| ValidateMacroGeneration | **377085 checks**, exit 0: original small-world v1 baseline pinned explicitly |
| ValidateRiverPlanning | **94885 checks**, exit 0: original small-world legacy river baseline pinned explicitly |
| ValidateFeatureReservations | **175 checks**, exit 0 |

Existing camera interpolation warning remains; no engine errors in the passing suites above. Other broader previously documented seasonal/native runner issues are not claimed fixed. Tests exercise production systems; no new practice scene is needed to enable the feature. Separate regression scenes remain developer checks only. Source whitespace and changed Markdown link checks passed. After rebasing onto harvesting revision `f92d804`, the ocean, geography-first, streamed-river and streamed-water suites passed again with the counts above. Save fixtures capture the current schema-3 inventory while legacy geography restoration remains covered.

## Lifetime and performance

Wake: one new-world plan plus existing chunk prepare/publish/visibility/weather events. Stop: planner returns immutable values; temporary masks/queues die; chunk cancellation/unload releases fields and last resident region. Idle: no ocean-specific processing, timers, shaders or new manager; existing shared visible liquid animation/wading/rain retains its normal wake/sleep rules.

Full world ocean geometry is at most two bounded polygons, not a whole-ocean mesh/texture or physics grid. Near tiles use world-aligned saturated distance fields, local edges and one parity scan per row with eight-row yields. Interior ocean chunks work even with no coast inside their field halo. Shared polygon validation rejects disjoint edge bounds before distance/intersection checks. Nearby scenery remains bounded; macro textures/blueprint arrays grow with 58102 coarse cells, not all detailed world tiles. Startup and individual batches remain synchronous and the stream budget is soft. No target CPU/GPU/memory or FPS improvement is claimed; target measurements remain required.

## User and local handoff

Use **SCENES/World/World.tscn → F6 → F7** or F5 → New Campaign. The ocean is dark blue; the whole map fits within the panel. Water is far from the protected starting area by design. Continue of an older campaign retains the older size/geography; use a new disposable World session to see the enlarged world/ocean.

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Generation/ValidateOceanGeneration.tscn --fixed-fps 60
godot --headless --path . TESTS/Generation/ValidateGeographyFirst.tscn
godot --headless --path . TESTS/Water/ValidateStreamedRiver.tscn --fixed-fps 60
godot --headless --path . TESTS/Water/ValidateStreamedWater.tscn --fixed-fps 60
godot --headless --path . TESTS/Generation/ValidateMacroGeneration.tscn
godot --headless --path . TESTS/Water/ValidateRiverPlanning.tscn
godot --headless --path . TESTS/Generation/ValidateFeatureReservations.tscn
```

Verify map fit at different window sizes, coast zoom/seams, no trees/rocks/plants in ocean, navigation/immersion and separate-process old/new campaign restart. Profile startup/F7 map creation separately from shoreline traversal, deep-ocean movement, unload/return, idle/dry/rain states and memory growth. Old saves must not be reset to test this. WG4 shores/shallows, WG6 currents and WG7 swimming remain planned; current ocean uses existing wading.
