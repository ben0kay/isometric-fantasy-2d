# Combat fixtures

`ValidateCombatFoundation.tscn` runs C1 deterministic health/stats/damage and production player/entity integration. Run with Godot Mono headless after building. Exit 0 plus `C1 PASS` is success; any violated contract exits 1. No campaign storage is accessed. P4 save compatibility and player HP persistence are exercised separately in TESTS/Save. See COMBAT/C1_Validation.md for full results, commands and manual performance checks.

`ValidateMeleeExecution.tscn` runs C2 shared timing/stats/defense, moving geometry/misses, faction/self/friendly exclusion, walls, dense-query fail-closed behavior, actor death/suspension/queued-target safety, real Oak/Pink tool feedback, actual input→axe and skeleton→player adapters, and health-bar settling. It instantiates production components without a WorldScene or campaign and exits 1 on failure. See COMBAT/C2_Validation.md for exact commands, baseline save compatibility and broader regression limitations.
