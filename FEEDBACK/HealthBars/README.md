# Shared health bars

Ported/adapted from `isometric-new-2d/VISUALS/Feedback/HealthBars`, preserving the **HealthBars settings + world-space component** structure. Uses C1's existing `ActorHealth` for both `Player.tscn` and `Entity.tscn`; it does **not** add an independent Health or Defense component. Defense remains in `COMBAT/Damage/` and effective stats.

`SharedHealthBar.cs` binds after owner initialization to its existing health object and rendered Sprite2D bounds. It hides at full HP by default, shows on damage, animates fill briefly, and hides on death. Restoring an injured actor updates immediately without replaying an impact animation. Width scales logarithmically with maximum HP and clamps to bounded min/max. `DefaultHealthBarSettings.tres` provides editor tuning for sizing/placement/colors/visibility/transition, with optional per-instance settings overrides.

No idle processing or global actor scan; each attached bar only receives its actor's HP events, and unsubscribes when its scene exits. The bar is attached to the actor, so streamed/despawned actors carry their presentation with them. Player HP is shown above the player when injured; this is *not* the future player HUD.

**Validation outstanding:** This GitHub-authored pass has not been Godot-build/runtime-verified. Build with Godot 4.7.2 Mono/.NET 8; confirm full-health hidden, player 100 HP takes damage, skeleton 3 HP displays bar, damage/heal/restore/death, actor sprite offsets, Y sort, P4 Continue of injured player/skeleton, and removal on scene exit. Run existing C1, P4 and SO2 suites. New scene-script references may alter P4 content fingerprints; inspect compatibility behavior before changing any durable campaign save files.
