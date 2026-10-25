# Central player input — C2.5

`PlayerInput` owns all mappings and emits intent; it never moves actors or computes damage.

| Control | Intent |
| --- | --- |
| I / Esc | Toggle backpack / close backpack first |
| Arrows | Normalized movement |
| LMB | Equipped primary forward slash |
| MMB | Equipped alternate forward thrust |
| RMB | No shield: tap for visual hop; shield equipped: hold directional guard |
| 1 / 2 | Select starter axe / temporary sword |
| 3 | Toggle shield-equipped flag (starts OFF) |
| Right Shift | Hold sprint; double-tap within 0.28s while holding arrows to dodge in movement direction |

No arrow direction means no dodge; repeats/left Shift do not trigger the shift action. UI-consumed presses do not start gameplay actions. Right-button/right-Shift releases are handled separately so UI cannot trap an active guard/sprint; pause/window focus loss clears held state and tap history. The input script unsubscribes its window event on exit.

Equipment/action owner rejects invalid overlaps with no input queue. After a dodge cancels guard or a guard press is rejected during attack, release/repress RMB to guard. Key bindings are temporary direct test controls, not a hotbar or rebind UI.

UI2: named `player_inventory` defaults to I. I/Esc inventory intent is handled before GUI focus and campaign unhandled-key input; it does not toggle while the tree is paused. While inventory is open, movement reads zero and new attack/utility/starter/sprint/dodge presses are suppressed. Opening releases held guard/sprint and cancels voluntary dodge through a movement-owner signal. Already committed attack/hop and external damage reactions continue. Closing allows held arrows again; guard/sprint require a fresh press. HUD teardown releases the gate. No UI-owned polling or item state.

Inventory foundation: optional HotbarSelectionEnabled routes 1–9 to HotbarSelectionRequested instead of temporary StarterSelectionRequested. Defaults OFF and is enabled only by InventoryPractice. InventoryOpen continues to suppress both paths. Production controls remain unchanged until UI3 save/ownership integration.

H1: player_interact defaults to E. PickupRequested is emitted only while PickupInteractionEnabled is explicitly set by PlayerPickupInteraction (currently harvesting practice only); inventory-open suppression still applies. No repeat/held-key pickup polling.
