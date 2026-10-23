# Shared AI

`GroundChase` is the optional generic pursuit motor already used by the skeleton. C2 adds a small melee intent adapter: within receiving reach it requests the same `AttackExecution` as the player. Wind-up/contact/recovery hold the actor still through this sole motor; cooldown permits existing chase behavior. Navigation/pathing/stop distance remain unchanged. A null navigation grid permits nearby attacks but no movement, useful for isolated locations/tests.

`EntityDefinition.ChasePlayer` activates pursuit through explicit target/navigation injection. `MeleeEnabled` independently enables attack requests; optional `MeleeAttack` overrides `COMBAT/Weapons/DefaultEnemyMelee.tres`. Passive actors have neither combat callbacks nor an executor. Active pursuit retains its existing physics callback, but distant/idle decisions perform no contact queries. Scene process-mode suspension cancels stale wind-ups, and death stops pursuit/attacks together. Unload disposes query resources.

Roaming, fleeing, guarding, populations and simulation LOD remain future work; this is not an A-Life system or a dedicated skeleton controller.

AC2: suspension additionally clears velocity/cached pursuit route. Moving enemies check the active navigation rectangle and loaded destination chunk before their existing MoveAndSlide. They do not request the player camera preload ring. One navigation grid is shared by the authored test actors; rebuild cadence stays with the existing streamer. Runtime acceptance remains pending.

ES3: `EngagementPositioning.cs` and `EngagementProfile.cs` provide bounded Direct/Spread approach selection and transient target-scoped reservations. They never move bodies or own callbacks. GroundChase consumes the chosen destination after existing attack/reaction gates. Global settings live under Shared/Movement/MovementMasterConfig; optional per-entity overrides live on EntityDefinition. See [ES3 validation](../Movement/ES3_Validation.md).

## R1 ranged intent

Optional EntityDefinition.Ranged takes precedence over melee intent, even if MeleeEnabled is true. `RangedCombatAI` is plain state, driven by the existing GroundChase physics callback; it never moves an actor. Bind a scene-local ProjectileSpawner through Entity.ConfigureRanged and target/navigation through ConfigureNavigation. Unbound ranged actors sleep until services are injected.

Within preferred firing range, the actor draws, locks direction for the final authored aim-lock window and releases a straight R0 shot. Attack-speed stats snapshot wind-up/cooldown durations when a draw begins; projectile stats snapshot at release. Min/max range add hysteresis without perpetual fleeing: too close holds fire rather than starting a retreat loop. Cooldown holds a valid firing position. LOS is throttled at5Hz with instance phase offsets; release performs a fresh locked-line check. Blocked actors test up to4 side points/routes at most once per second. Melee engagement reservations are not used for ranged positions; moving pursuit still uses local separation and the normal static-route motor.

Interrupting stagger cancels the pending shot and consumes its cooldown. Non-interrupting recoil pauses the draw and never fires from the reaction branch. Death/disable/target loss cancel intent. Navigation reconfiguration with the same target keeps native/visual helpers, cancelling only pending intent; target/service/profile changes recreate the optional module. Scalar ranged-profile edits take effect on explicit ConfigureRanged/reload, not per-frame resource polling.

EnemyRangedEnabled in CombatMasterConfig and RangedCombatProfile.Enabled independently gate firing/movement without LOS work. An active chase motor still checks those switches to allow reactivation; no new passive/idle Node callback was added. R2 can add compact F9 controls through explicit update commands.

[Practice, exact results and limits](../../../COMBAT/Projectiles/R1_Validation.md).
