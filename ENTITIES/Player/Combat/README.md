# Player combat adapter

C1/C2 player receiving uses `PlayerMovement`'s existing shared HP/stats and `ICombatTarget`; input stays centralized in `PlayerInput`. `EquippedTool` is the current attack-request/presentation adapter, using the same COMBAT/Weapons executor as enemy pursuit. No duplicate player-only timing, damage engine, HP or physics motor belongs here.

Current sprint/dodge and health bars are preserved. C3–C5 will add feedback, movement tuning, telegraphs and a complete encounter/retry loop. See [combat plan](../../../COMBAT/COMBAT_IMPLEMENTATION.md).

**Next planned slice:** [C2.5 player actions, dual attacks and starter equipment](../../../COMBAT/C2_5_PlayerActions.md) (Codex handoff; no implementation yet).
