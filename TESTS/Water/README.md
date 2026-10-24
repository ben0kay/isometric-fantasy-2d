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

- `RiverPractice.tscn`: F6 starts beside a middle section; R visits five fixed stations, F7 draws the saved route, F9 controls shared weather. Cloned config opts into two lakes and one cross-biome river.
- `ValidateRiverPlanning.tscn`: 12 seeds, deterministic repetition/order/slot independence, positive continuous corridor/variable width, protected areas, cross-biome eligibility, hard/soft exclusions, forbidden world-spanning barrier, sparse corridor chunks, single-biome fallback and frozen JSON/corruption/version checks. Smooth-shape assertions bound local sample spacing/turn angles, retain version-2 restore, reject dense geometry under old versions, and compare navigation to exact shoreline clearance.
- `ValidateStreamedRiver.tscn`: real production source/middle/terminal traversal, shared wading/scenery/field binding, disk checkpoint and real Continue with the new-world planner disabled. Validates schema-7 identity and rejection under schema 4/5/6; sampled chunk fields must match exact shoreline distance.

[River settings, exact results and target-PC commands](../../WORLD/Generation/Liquids/Rivers/WG3_Validation.md).
