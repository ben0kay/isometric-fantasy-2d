# R0 projectile fixture

`ValidateProjectiles.tscn` runs 75 checks against the production projectile runtime and actual player/entity combat receivers. `ProjectileObstacleProbe` is a thin solid tool receiver proving arrows stop without harvesting/wood-chopping.

Run `godot --headless --path . --max-fps 60 res://TESTS/Projectiles/ValidateProjectiles.tscn` from repo root after building. Expected: R0 PASS 75 and exit 0. See [validation and limitations](../../COMBAT/Projectiles/R0_Validation.md).
