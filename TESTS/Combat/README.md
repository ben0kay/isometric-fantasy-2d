# Combat fixtures

`ValidateCombatFoundation.tscn` runs C1 deterministic health/stats/damage and production player/entity integration. Run with Godot Mono headless after building. Exit 0 plus `C1 PASS` is success; any violated contract exits 1. No campaign storage is accessed. P4 save compatibility and player HP persistence are exercised separately in TESTS/Save. See COMBAT/C1_Validation.md for full results, commands and manual performance checks.
