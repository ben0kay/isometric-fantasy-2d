# P0–P3 checks

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
