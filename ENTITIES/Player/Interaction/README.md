# Player / Interaction

Future player-specific detection/selection of interactables and requests to use them (doors, containers, furniture, dialogue, etc.).

Keep actual object behavior with the interacted object's owning system; the player should ask to interact, not implement every object's behavior.

**Current compatibility helper:** `ToolHitQuery` / `IToolHitTarget` provide the original one-shot tool API, retained for tree tests and existing callers. C2 production swings use `COMBAT/HitDetection/MeleeHitQuery` through the shared executor; they do not call this legacy group scan. General doors/containers/dialogue selection remains planned.

H1–H3: PlayerPickupInteraction explicitly binds one player, existing InventoryStorage and disposable WorldDropSpawner. It enables named player_interact (E) through PlayerInput, collects one nearest settled/visible-in-range drop per request, and unsubscribes/disables itself on exit. No polling, new inventory or production auto-wiring. General interaction selection remains planned.
