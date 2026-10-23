# Player / Interaction

Future player-specific detection/selection of interactables and requests to use them (doors, containers, furniture, dialogue, etc.).

Keep actual object behavior with the interacted object's owning system; the player should ask to interact, not implement every object's behavior.

**Current compatibility helper:** `ToolHitQuery` / `IToolHitTarget` provide the original one-shot tool API, retained for tree tests and existing callers. C2 production swings use `COMBAT/HitDetection/MeleeHitQuery` through the shared executor; they do not call this legacy group scan. General doors/containers/dialogue selection remains planned.
