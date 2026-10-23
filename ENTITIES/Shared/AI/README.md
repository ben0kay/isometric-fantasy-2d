# Shared AI

`GroundChase` is the optional generic pursuit motor already used by the skeleton. C2 adds a small melee intent adapter: within receiving reach it requests the same `AttackExecution` as the player. Wind-up/contact/recovery hold the actor still through this sole motor; cooldown permits existing chase behavior. Navigation/pathing/stop distance remain unchanged. A null navigation grid permits nearby attacks but no movement, useful for isolated locations/tests.

`EntityDefinition.ChasePlayer` activates pursuit through explicit target/navigation injection. `MeleeEnabled` independently enables attack requests; optional `MeleeAttack` overrides `COMBAT/Weapons/DefaultEnemyMelee.tres`. Passive actors have neither combat callbacks nor an executor. Active pursuit retains its existing physics callback, but distant/idle decisions perform no contact queries. Scene process-mode suspension cancels stale wind-ups, and death stops pursuit/attacks together. Unload disposes query resources.

Roaming, fleeing, guarding, populations and simulation LOD remain future work; this is not an A-Life system or a dedicated skeleton controller.
