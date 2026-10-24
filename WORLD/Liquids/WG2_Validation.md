# WG2 streamed generated lakes — 24 October 2026

**Implemented; Windows graphical/performance acceptance pending.** WG3 rivers, WG4 detailed banks/carving, WG6 currents and WG7 swimming remain planned. This phase renders the WG1 saved polygons with the existing pond-style water/shore shader and player wading/splash behavior.

## Behavior and ownership

- `WorldScene.GeneratedWater` composes a single runtime under TerrainRoot when blueprint schema is 5. `GeneratedWaterRuntime` resolves the canonical Water.tres once, then consumes chunk prepare/publish/release events from the existing WorldChunkStream. Unknown liquid IDs fail explicitly; no silent visual fallback.
- `WorldLiquidRegion` now accepts a supplied PlannedWaterBody. Its full polygon feeds cached edge/distance queries, cropped scalar fields, player immersion and rain. The authored pond retains its original 96-point shape, field and visual path.
- Each liquid tile occupies exactly its ground chunk rectangle. Scalar samples use a global four-unit grid with a halo; shader samples by world coordinates. Full-polygon shoreline distance and one body animation phase avoid artificial chunk shores, restarted wave phases and overlapping water quads. Detailed terrain carving and expanded shoreline art remain WG4.
- Scenery candidate rejection consults full frozen geometry plus existing radius/shore margins before priority thinning, including across unloaded neighboring chunks. Local enemy navigation rasterizes the same polygon through an optional generic obstacle argument; player collision is unchanged. WG2 lakes are fully wadeable, with slowdown/visual immersion, not deep-swim barriers.
- The existing single WorldLiquidWading switches among fixed/generated regions; there are no competing speed multipliers or player sprite-material owners. Ground rain splashes skip liquid points; pooled liquid splashes remain region-owned and use shared weather. Player waterline rings reuse the existing appearance.
- Source scene/Resource dependency text is unchanged. Runtime scripts do not depend on DEBUG/UI or practice scenes. Water generation stays opt-in in normal World; the practice fixture explicitly enables it.

## Save/version policy

New worlds with enabled generation and at least one planned lake write blueprint **schema 5** before streaming. Schema 5 requires nonempty validated water data; pre-WG2 code rejects schema 5 instead of silently ignoring playable lakes/scenery exclusions. Existing schema-4 campaigns retain schema 4, their exact biome/feature hashes and their previous scenery behavior. Schema-4 WG1 water metadata remains metadata only; Continue never promotes it into water or reruns the planner. No migration/reset or insertion into old worlds is performed. New worlds with no generated bodies continue using schema 4.

Continue uses frozen water version/seed/IDs/polygon hash and validates it before materialization. A changed new-world recipe does not change a saved lake. The existing strict authored-content fingerprint remains active; editing generation `.tres` files still requires F6/new campaign or a separately designed migration. No bypass was added.

## Performance and lifecycle

A 512-unit tile uses about 130×130 Rf samples (roughly 66 KB scalar data, plus GPU/material overhead). At the maximum 2048 chunk size it remains bounded at about 514×514 samples. Field work yields after eight rows; startup still prepares the initial active area synchronously, as existing streaming does. GPU upload and an individual row batch are not preemptible; the existing application budget remains soft. This pass does not claim hitch-free target performance.

Only nearby loaded chunks own water quads/fields; nearby sections share one body query cache, one clock and one rain pool. Empty chunks own no surface data. No whole-lake giant distance texture or whole-world water mesh/cache is created. No physics walls or per-water-tile callbacks exist. Static palette uniforms are cached; unchanged uniforms are not rewritten. Body animation/camera culling runs only while resident animated/raining water is enabled. Offscreen bodies receive no shader animation updates; dry rain pools stop. Shared config changes explicitly wake/stop the runtime. One player immersion physics query remains; far from water it uses bounding rejection and no wading draw callback. Unload/cancellation and scene teardown unregister events and release fields/materials/regions.

## Validation

Completed on this runner:

- All production/test C# sources compiled through Roslyn 4.11 with .NET 8 reference assemblies, Godot 4.5.1 API and the shipped Godot SourceGenerators: **0 errors / 0 warnings**. The repository retains `Godot.NET.Sdk/4.7.2` and `net8.0` unchanged.
- Normal `dotnet build` was attempted but the host .NET CLI/MSBuild failed before project compilation in System.Diagnostics.Process process-information access. This is a runner limitation; normal target-PC `dotnet build` is still deferred. The direct compiler check is not represented as a normal SDK build.
- Linux Godot **4.5.1 Mono**, .NET **8.0.21** runtime: `ValidateStreamedWater.tscn --fixed-fps 60` passed **214 checks**, exit 0. Includes world-space tile seam samples, unpublished/cancelled tiles, unload/reload resource ownership, nav avoidance, actual streamed scenery rejection, player slowdown/immersion, original fixed-pond wading, dry/static/rain sleep/wake, disk identity and real Continue scenes for schema-5 playable water, schema-4 WG1 metadata and schema-4 null water. These are same-process Continue scenarios after disk round-trip, not a separate-process restart test.
- Existing `ValidateWaterPlanning.tscn`: **1415 checks**, exit 0.
- Existing `ValidateFeatureReservations.tscn`: **175 checks**, exit 0.
- `WaterPlanningPractice.tscn --fixed-fps 60 --quit-after 500`: headless smoke passed, exit 0. Engine logs only the existing camera physics-interpolation warning in these runtime tests; no runtime errors were observed.
- Editor import completed with exit 0 but emitted host .NET SDK/MSBuild tooling diagnostics; it is not claimed as a clean target-editor check. Resource/scene loading was also exercised by runtime fixtures. No production scene or `.tres` text changed.
- `git diff --check` passed.

Deferred: normal target Windows Godot 4.7.2 build/import, actual graphical shore/seam/zoom/rain inspection, separate-process campaign restart, profiler FPS/allocations/memory/hitch measurements and broad unrelated regression suites. Headless scalar-seam equality and callback-state assertions do not establish graphical correctness or FPS. The imported editor-generated unrelated metadata was removed from this change.

## Local test route

1. Open `TESTS/Water/WaterPlanningPractice.tscn` and press **F6**. It enables three frozen lakes at seed 42 and places you just outside the first shoreline. Nearby streaming may take a moment after this disposable teleport.
2. Walk into/out of water. Confirm slowdown, submerged feet and waterline rings settle correctly, with no trees/rocks rooted in the water.
3. Press **L** to visit the next lake; cycle back to verify stable shape, no duplicates, and released/recreated water after travel. F7 shows blue lake footprints and playable/metadata status. F9 retains ordinary wind/rain controls; test rain on/off and zoom/movement across tile boundaries.
4. Check the authored pond in a normal World scene remains functional. Normal World generation stays off until WaterGenerationMasterConfig is explicitly enabled on the generation config; ordinary F6 World appearance remains unchanged.
5. Test an opted-in new campaign save/Continue/restart. Do not overwrite a valued existing campaign. Confirm an existing schema-4 campaign does not acquire generated lakes.

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --fixed-fps 60 --path . TESTS/Water/ValidateStreamedWater.tscn
godot --headless --path . TESTS/Water/ValidateWaterPlanning.tscn
godot --headless --path . TESTS/Generation/ValidateFeatureReservations.tscn
```

Use the installed Godot Mono executable path. Profile startup, steady dry/animated/rainy states, zero intensity, reactivation, camera-edge visibility, chunk crossing and repeated travel/return separately. Inspect native renderer shoreline continuity/scale, rain placement and frame-time/memory peaks. No broad environmental/combat suite or graphical acceptance is implied by focused checks.
