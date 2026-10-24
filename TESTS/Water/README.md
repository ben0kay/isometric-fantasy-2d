# Water planning tests — WG0/WG1

- `WaterPlanningPractice.tscn`: F6 launches a disposable seeded production World with three planned lakes. F7 shows blue footprints. No generated water is rendered before WG2. Production Resources are cloned, never written.
- `ValidateWaterPlanning.tscn`: deterministic 12-seed repeat/order/slot checks, protected reservations, fixed-pond separation, finite outlines, frozen copies, chunk queries, JSON and disk checkpoint round trips, corruption/version rejection, null/disabled/zero suitability, hard exclusions including an enclosed forbidden biome, and single-biome fallback. Disk tests use a unique temporary directory and clean only that directory.

From project root on the target PC:

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Water/ValidateWaterPlanning.tscn
godot --headless --path . TESTS/Generation/ValidateFeatureReservations.tscn
godot --headless --path . TESTS/Generation/ValidateMacroGeneration.tscn
```

Use the installed Godot Mono executable's actual name/path. Expect `WG1 PASS`, `P2 PASS` and the macro fixture's success log; nonzero exit or engine errors require investigation. Graphical check: open practice, F6→F7; inspect blue organic footprints, close F7 and confirm existing pond/wading, scenery and controls remain functional. Profile startup separately from steady-state and chunk crossing; no new water callbacks should run after planning. Check existing campaign Continue retains no generated lakes; newly opted-in test campaigns must preserve exact plan after restart. See the owning validation note for runner versions and results.
