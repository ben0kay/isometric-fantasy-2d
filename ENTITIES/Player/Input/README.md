# Central player input — C2.5

`PlayerInput` owns all mappings and emits intent; it never moves actors or computes damage.

| Control | Intent |
| --- | --- |
| Arrows | Normalized movement |
| LMB | Equipped primary forward slash |
| MMB | Equipped alternate forward thrust |
| RMB | No shield: tap for visual hop; shield equipped: hold directional guard |
| 1 / 2 | Select starter axe / temporary sword |
| 3 | Toggle shield-equipped flag (starts OFF) |
| Right Shift | Hold sprint; double-tap within 0.28s while holding arrows to dodge in movement direction |

No arrow direction means no dodge; repeats/left Shift do not trigger the shift action. UI-consumed presses do not start gameplay actions. Right-button/right-Shift releases are handled separately so UI cannot trap an active guard/sprint; pause/window focus loss clears held state and tap history. The input script unsubscribes its window event on exit.

Equipment/action owner rejects invalid overlaps with no input queue. After a dodge cancels guard or a guard press is rejected during attack, release/repress RMB to guard. Key bindings are temporary direct test controls, not a hotbar or rebind UI.
