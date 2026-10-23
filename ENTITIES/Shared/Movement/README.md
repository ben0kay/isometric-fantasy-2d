# Shared / Movement

Actor collision is complete for the agreed current scope; revisit [ActorCollisionPlan.md](ActorCollisionPlan.md) if playtesting reveals a problem. [AC2](AC2_Validation.md) documents the disposable five-skeleton F6 setup; [AC4](AC4_Validation.md) records targeted passes and broader engine shutdown limitations.

## C3.2 — Knockback and stagger

`Knockback.cs` is plain transient state shared by PlayerMovement and Entity/GroundChase. It has no node, timer, physics query or movement callback of its own. Confirmed damaging melee contacts pass the snapshotted attack values and locked aim directly to `IReactionTarget`. Zero applied damage, props and killing blows do not start reactions. Positive chip damage can react even when guard reduced the hit.

Attack Resources expose distance, push duration, stagger duration and explicit attack interruption. PlayerMasterConfig and EntityDefinition expose separate knockback/stagger resistance (0–1) and total immunity. Species, roles and factions require no custom reaction classes. A heavy creature can resist pushing while retaining stagger, or vice versa.

Push decelerates linearly; its unobstructed integrated distance matches authored distance. Existing motors pause locomotion during push/stagger and use their sole MoveAndSlide call, retaining scenery/actor collision and loaded-area guards. Actual distance can be smaller. The newest hit replaces remaining push direction/distance; stagger refreshes to the longer remaining duration. A reaction burst is capped at 0.6 seconds, followed by 0.25 seconds of reaction immunity; damage still applies during that recovery. Forces never sum.

Stagger blocks new attacks/actions. Only an attack profile explicitly enabling InterruptAttack suppresses an already committed swing/contact and ends its presentation; remaining attack time/cooldown is retained. Default swipe and enemy attacks do not interrupt; axe prod does. Player recoil ends guard and dodge; an existing visual hop finishes normally while its feet stay grounded. Non-interrupting hits allow an already committed attack to finish.

Death, disabled ownership and unloading discard transient state; it is never saved. Passive entities wake their existing GroundChase motor only for a reaction/recovery window, then sleep. Active pursuers and the player use existing physics callbacks; no extra world scans, allocations per reaction or per-species loops. No target-PC performance measurement is claimed.

See [C3.2 validation and tuning](../../../COMBAT/C3_2_Validation.md).
