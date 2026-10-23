# Shared / Movement

Future shared walking, swimming, flying and other locomotion components. Keep player-only controls in Player/Input and Player/Movement unless truly reusable.

**Status:** AC0 audit complete; AC1/AC2 targeted headless checks passed, with initial user open-terrain acceptance. AC3 corridors deferred. AC4 executed with broader world shutdown limitations; see [AC4 results](AC4_Validation.md).

## Upcoming C3.2 — Shared knockback/stagger

See [C3.2 implementation handoff](../../../COMBAT/C3_Implementation.md#c32--shared-knockback-and-stagger-codex-implementation-handoff). Proposed `Knockback.cs` remains **unimplemented**. The shared helper must feed existing PlayerMovement/GroundChase physics rather than become a new mover; reconcile [actor collision](ActorCollisionPlan.md) first. Supports attack-specific force and species resistances through Resources, with transient sleeping state.

[AC2 validation and practice setup](AC2_Validation.md) covers dead-player blocking, inherited disabled collision removal, enemy movement bounds, and five disposable F6 skeletons under a single toggle.
