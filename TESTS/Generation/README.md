# P0/P1 checks

Build the C# project in the Godot .NET editor first. From the repository root:

```sh
godot --headless --path . res://TESTS/Generation/ValidateMacroGeneration.tscn
godot --headless --path . --script res://TESTS/Generation/measure_preview.gd
```

Use your Godot .NET executable name/path. The first command checks macro correctness/authoring and the detached manifest contract; failures exit nonzero. Four biome types in the test are temporary Resource data only, not added game content. The second verifies unchanged production scenery with/without P1 and prints startup, scenery, sampled peak node/memory and frame-monitor statistics. Headless frame timings are diagnostic only; profile the actual renderer on target hardware.

No save files are written. The original DEBUG main scene remains unchanged. F6 World then F8 opens the optional map overview. See WORLD/Generation/World/P0_P1_Audit.md for scope and limitations.
