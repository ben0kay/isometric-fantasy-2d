# Axe tool

`Axe.tscn` supplies the shared EquippedTool adapter and compatibility hand/aim config. Production `StarterAxe.tres` owns the equipped PNG art, grip, scale and two attack references; edit that Resource for visible item changes. Its sprite scale is now **0.144 × 0.144**, exactly 10% smaller than the previous 0.16. Grip offset/orientation and gameplay reach remain unchanged. The scene's 0.16 preview scale is overridden by the equipped definition at runtime.

LMB uses `PrimaryAttack.tres`: 11 damage, 52 reach, 0.38s + 0.15s cooldown, 15px push / 0.12s stagger. MMB uses `SecondaryAttack.tres`: 5 damage, 60 reach, 0.28s + 0.45s cooldown, 55px push / 0.30s stagger with interruption. Both retain prop hits through shared melee execution. The canonical skeleton has 50 HP; F6 World offers five disposable practice skeletons.

The cosmetic sweep now carries momentum through contact; the thrust smoothly aligns, draws back slightly and extends before withdrawal. Both blend back to current aim after completion/interruption. No new physics mover, attack timing or hit rules. See [shared equipment motion](../../../ENTITIES/Player/Equipment/README.md) and [C3.2 validation](../../../COMBAT/C3_2_Validation.md). Latest motion/scale polish still requires local runtime and visual validation.
