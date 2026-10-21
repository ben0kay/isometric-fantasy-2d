# UI

> **Status:** Planning/folder foundation only. This pass does not implement systems or content.

Reusable user interface and screen presentation for the fantasy game. Input/data ownership remains with player and gameplay systems; UI observes and displays that data.

## Folder map

- [HUD/](HUD/README.md): Player needs, stamina, health, quickbar, time, conditions and world notifications.
- [Menus/](Menus/README.md): Pause, character, options, main menu and other screens.
- [Inventory/](Inventory/README.md): Inventory grids, item inspection, drag-and-drop and containers as UI only; inventory state lives in ENTITIES/Player/Inventory or future shared systems..
- [Tooltips/](Tooltips/README.md): Reusable item, status, recipe and object tooltips with shared display rules..
- [Crafting/](Crafting/README.md): Crafting and recipe presentation; crafting rules belong in SYSTEMS/Crafting..

## Conventions

- `UIMasterConfig.cs` and `DefaultUIConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
