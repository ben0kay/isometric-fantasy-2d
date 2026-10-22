# UI

Reusable user interface and screen presentation for the fantasy game. Input/data ownership remains with player and gameplay systems; UI observes and displays that data.

## Folder map

- [HUD/](HUD/README.md): Player needs, stamina, health, quickbar, time, conditions and world notifications; [Gameplay/](HUD/Gameplay/README.md) for the planned in-game HUD.
- [Menus/](Menus/README.md): Pause, character, options, main menu and other screens.
- [Inventory/](Inventory/README.md): Player backpack and container interfaces; see [PLAYER_INVENTORY.md](Inventory/PLAYER_INVENTORY.md) and [Containers/](Inventory/Containers/README.md).
- [Tooltips/](Tooltips/README.md): Reusable item, status, recipe and object tooltips with shared display rules.
- [Crafting/](Crafting/README.md): Crafting and recipe presentation; crafting rules belong in SYSTEMS/Crafting.

## Conventions

- `UIMasterConfig.cs` and `DefaultUIConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Future external Avalonia toolbox should author item/container Resources rather than per-item UI scenes.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
