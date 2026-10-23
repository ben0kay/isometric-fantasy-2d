# Shared / Movement

Future shared walking, swimming, flying and other locomotion components. Keep player-only controls in Player/Input and Player/Movement unless truly reusable.

**Status:** AC0 source audit complete; AC1 explicit WorldSolids/Actors layers and melee mask implemented, awaiting local validation. AC2–AC4 remain pending. No new movement component was added. See [ActorCollisionPlan.md](ActorCollisionPlan.md) for explicit player/entity body-blocking layers, C2 compatibility and implementation/test instructions.

## Upcoming C3.2 — Shared knockback/stagger

See [C3.2 implementation handoff](../../../COMBAT/C3_Implementation.md#c32--shared-knockback-and-stagger-codex-implementation-handoff). Proposed `Knockback.cs` remains **unimplemented**. The shared helper must feed existing PlayerMovement/GroundChase physics rather than become a new mover; reconcile [actor collision](ActorCollisionPlan.md) first. Supports attack-specific force and species resistances through Resources, with transient sleeping state.
