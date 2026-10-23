# Player movement

`PlayerMovement.cs` remains the authoritative CharacterBody2D physics motor of `Player.tscn`: reads intentions from `PlayerInput`, uses `PlayerMasterConfig` and cached C1 effective movement stat, respects surface speed modifiers, the P3 `Streaming.CanMoveTo` boundary and `MoveAndSlide` collision.

**Movement feel mini-pass (2026-10-22):** Right Shift sprint at 1.55× walking speed while held. Double-tap right Shift (two separate key presses within 0.28s) starts a held-arrow-direction 0.16s dodge at 480 world units/s with a 0.6s cooldown, all Resource-tunable. Dodge takes precedence over walk/sprint for its active window; no sprite animation, i-frames, stamina, extra Node, separate physics processor or scene-tree reparenting. The existing movement motor continues applying collisions/streaming guards. Sprint/queued tap state resets on death.

**Validation:** code authored remotely; Godot/.NET build and real Windows play test **not run in this change**. Check right-vs-left Shift, single-hold vs double-tap, diagonal and no-direction behavior, collision/tree/streaming boundaries, health death/revive, attack during dodge and camera behaviour. Later C4 expands acceleration/facing/attack modifiers and can rebalance these defaults.

## C2.5 action integration

The existing single motor now consults `PlayerActions`. Guarding replaces sprint with `GuardProfile.MovementMultiplier` (default 0.45× walk), still multiplied by actor/surface stats. Valid dodge cancels guard, and committed attacks/hops cannot be interrupted by dodge. No duplicate velocity integrator.

No-shield RMB starts a 12-world-unit, 0.35s visual hop with 0.45s extra cooldown. It adjusts `Sprite.Offset` in texture units, composing independently with snow/water's existing `Sprite.Position` owner. Equipment follows its visual height. Root/body/collider/camera/Y-sort coordinate stay unchanged; there is no obstacle crossing, invulnerability or damage immunity. A nonpositive height/duration disables hop activation. Rendering offset restores exactly on landing/death/suspension. The existing physics callback updates hop math only while active and decrements its cooldown; no extra Node/timer/idle animation callback. Configurable under PlayerMasterConfig.

C2.5 adds headless movement/guard/hop/priority tests; target-PC graphical feel and original sprint/dodge playtesting remain manual. See COMBAT/C2_5_Validation.md.
