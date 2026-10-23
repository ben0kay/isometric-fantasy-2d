# Player movement

`PlayerMovement.cs` remains the authoritative CharacterBody2D physics motor of `Player.tscn`: reads intentions from `PlayerInput`, uses `PlayerMasterConfig` and cached C1 effective movement stat, respects surface speed modifiers, the P3 `Streaming.CanMoveTo` boundary and `MoveAndSlide` collision.

**Movement feel mini-pass (2026-10-22):** Right Shift sprint at 1.55× walking speed while held. Double-tap right Shift (two separate key presses within 0.28s) starts a held-arrow-direction 0.16s dodge at 480 world units/s with a 0.6s cooldown, all Resource-tunable. Dodge takes precedence over walk/sprint for its active window; no sprite animation, i-frames, stamina, extra Node, separate physics processor or scene-tree reparenting. The existing movement motor continues applying collisions/streaming guards. Sprint/queued tap state resets on death.

**Validation:** code authored remotely; Godot/.NET build and real Windows play test **not run in this change**. Check right-vs-left Shift, single-hold vs double-tap, diagonal and no-direction behavior, collision/tree/streaming boundaries, health death/revive, attack during dodge and camera behaviour. Later C4 expands acceleration/facing/attack modifiers and can rebalance these defaults.
