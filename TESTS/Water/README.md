# Water planning tests — WG0/WG1

- `WaterPlanningPractice.tscn`: F6 launches a disposable seeded production World with three planned lakes. WG2 now renders the generated lakes; the fixture starts at the first shore and L cycles lakes. F7 shows blue footprints. Production generation Resources are cloned, never written.
- `ValidateWaterPlanning.tscn`: deterministic 12-seed repeat/order/slot checks, protected reservations, fixed-pond separation, finite outlines, frozen copies, chunk queries, JSON and disk checkpoint round trips, corruption/version rejection, null/disabled/zero suitability, hard exclusions including an enclosed forbidden biome, and single-biome fallback. Disk tests use a unique temporary directory and clean only that directory.

From project root on the target PC:

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Water/ValidateWaterPlanning.tscn
godot --headless --path . TESTS/Generation/ValidateFeatureReservations.tscn
godot --headless --path . TESTS/Generation/ValidateMacroGeneration.tscn
```

Use the installed Godot Mono executable's actual name/path. Expect `WG1 PASS`, `P2 PASS` and the macro fixture's success log; nonzero exit or engine errors require investigation. Graphical check: open practice with F6, walk into the generated lake, press L to cycle lakes, and F7 for blue footprints. Confirm wading, scenery exclusions, existing pond and controls remain functional. Profile startup separately from steady-state and chunk crossing; no new water callbacks should run after planning. Check existing campaign Continue retains no generated lakes; newly opted-in test campaigns must preserve exact plan after restart. See the owning validation note for runner versions and results.

`ValidateStreamedWater.tscn` exercises WG2 tile seam samples, supplied polygons, nav avoidance, partial-load cancellation, unload/return, actual player wading, scenery exclusions, rain/config sleep/wake, disk water identity and real Continue for playable/legacy/no-water worlds. Run with `godot --headless --fixed-fps 60 --path . TESTS/Water/ValidateStreamedWater.tscn`. See [WG2 results and manual checks](../../WORLD/Liquids/WG2_Validation.md).

## WG3 river fixtures

- `RiverPractice.tscn`: F6 starts beside a middle section; R visits five fixed stations, F7 draws the saved route, F9 controls shared weather. Cloned config opts into two lakes and one geography-first river; crossing multiple biome types is no longer forced.
- `ValidateRiverPlanning.tscn`: 12 seeds, deterministic repetition/order/slot independence, positive continuous corridor/variable width, protected areas, cross-biome eligibility, hard/soft exclusions, forbidden world-spanning barrier, sparse corridor chunks, single-biome fallback and frozen JSON/corruption/version checks. Smooth-shape assertions bound local sample spacing/turn angles, retain version-2 restore, reject dense geometry under old versions, and compare navigation to exact shoreline clearance.
- `ValidateStreamedRiver.tscn`: real production source/middle/terminal traversal, shared wading/scenery/field binding, disk checkpoint and real Continue with the new-world planner disabled. Validates schema-9 identity and rejection under older schema headers; sampled chunk fields must match exact shoreline distance.

[River settings, exact results and target-PC commands](../../WORLD/Generation/Liquids/Rivers/WG3_Validation.md).

P5a/P5b: production fixtures now create schema-8 worlds, test genuine legacy schema-4/5/7 Continue, and disable/change new-world water/hydrology before restoring to prove no reroll. Legacy planning fixtures retain the explicit biome-first API for original hash/eligibility regression.

P5c production defaults include an ocean and larger bounds; river integration uses that normal preset and validates water version 4. The lake-only fixture deliberately overrides the Water config and still creates no ocean. Small legacy macro/river fixtures pin the original bounds/cell size to retain their baseline meaning.
