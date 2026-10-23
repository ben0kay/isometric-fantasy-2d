# R0 projectile fixture

`ValidateProjectiles.tscn` runs 75 checks against the production projectile runtime and actual player/entity combat receivers. `ProjectileObstacleProbe` is a thin solid tool receiver proving arrows stop without harvesting/wood-chopping.

Run `godot --headless --path . --max-fps 60 res://TESTS/Projectiles/ValidateProjectiles.tscn` from repo root after building. Expected: R0 PASS 75 and exit 0. See [validation and limitations](../../COMBAT/Projectiles/R0_Validation.md).

## R1 archer

Open `ArcherPractice.tscn` and press F6 for a disposable room with one bow skeleton, player, cover and reset button. No campaign state is used.

`ValidateRangedCombat.tscn` checks ranged intent, aim lock/misses, LOS budgets, interruption, recoil pause/resume, service activation, shared-module reuse, multiple archers and lifecycle. `ValidateArcherWorld.tscn` checks real practice reset, World placement injection, lazy projectile ownership, crossed-chunk guards and unload.

[R1 results](../../COMBAT/Projectiles/R1_Validation.md).
