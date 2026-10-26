# Calendar and regional climate checks

After `dotnet build`, run `godot --headless --path . res://TESTS/Climate/ValidateSeasonalClimate.tscn`. Expected `SE4 BC3 CLIMATE PASS` / exit 0. This focused fixture exercises calendar, JSON state and pure climate math; it does not prove shader compilation, rendered particle masks or performance. Use the [local handoff](../../ENVIRONMENT/Climate/SE4_BC3_Validation.md) for F6 World/PreviewMap/Continue checks. No test run is claimed from the implementation environment.
