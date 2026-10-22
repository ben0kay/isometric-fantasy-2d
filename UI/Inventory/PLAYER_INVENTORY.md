# Player Inventory — Design Notes

> **Status: planning only.** No inventory UI, weight rules, item storage, containers, hotbar or gameplay mechanics are implemented by this note.

## Vision

A tactile, believable fantasy adventurer's backpack that feels more physical and immersive than a standard Minecraft-style grid, **without forcing the player to play inventory Tetris every time they gather wood, stone or ore**. Prioritize a pleasant gathering/building loop in an open-world survival sandbox.

## Proposed hybrid inventory

- **Presentation:** An opened travel backpack/rucksack illustration framing storage compartments, with nearby worn equipment and attached accessories. Art and visual presentation must be separate from storage logic.
- **Capacity:** Weight is the primary restriction; optionally model bulk or pack compartments later. Avoid a rigid one-item-per-tile rule for every material.
- **Resources:** Automatically group common harvested items into stacks or material bundles (e.g. a single displayed bundle of 12 logs, with total weight). Gathering multiple trees should remain practical.
- **Equipment:** Axes, pickaxes and other tools are individual items, with quick access from belt, hand, back or tool slots. Visually attached gear does not have to occupy the same presentation area as packed materials.
- **Special items:** Unique or large gear might optionally use a more physical slot/shape representation, but **do not make Tetris packing mandatory for all items**.
- **Encumbrance:** Consider a comfortable carry limit with graduated penalties rather than an immediate hard “inventory full” cutoff. Exact masses, thresholds and penalties are **undecided balancing**, not implementation requirements.
- **Quick access:** Prefer a compact contextual quick-belt or radial equipment selector over a permanent Minecraft-style nine-slot bar. Define interaction/controls later.
- **Convenience:** Auto-stacking, quick-transfer, sort, weight summaries and clear item/category inspection. A full backpack should not make collecting ordinary materials frustrating.

## Conceptual screen layout

```text
+----------------------------------------------------------+
| Character equipment           Open Adventurer's Pack     |
|  Head / torso / hands         Materials: Logs x12        |
|  Belt / back / pack           Ore x24, food x8           |
|  Equipped axe, waterskin      Tools / consumables        |
|                               Quick actions / sorting    |
|  Carry weight: 18 / 28 kg (illustrative values only)      |
+----------------------------------------------------------+
```

The actual window can be illustrated like a leather pack rather than a rectangular spreadsheet. Keep text legible and tooltips practical at different resolutions.

## Shared containers and looting

Player backpack and world containers (chests, barrels, corpses, dropped loot, carts, storage furniture) should **reuse the same item-slot/stack presentation components and transferable item data**. They can have *different skins/layouts*, capacities and interaction rules.

Plan future `UI/Inventory/Containers/` for shared container and loot **views**. Underlying inventories, transfers, item ownership, persistence and stack rules belong to reusable gameplay/data systems — not the UI. Avoid creating separate copies of item/stack logic for each chest type or for the player.

Potential open-container view: player's pack beside a chest, with quick-transfer and contextual inspect actions. Later support nested bags if justified; avoid accidental item duplication in transfer logic.

## Responsibility boundaries

- `UI/Inventory/`: overall inventory visual presentation; player screen design and reusable container views.
- `UI/HUD/Gameplay/`: in-world gameplay HUD, such as health/stamina/needs, equipped tool indicator, contextual quick-access (no inventory ownership).
- `ENTITIES/Player/Inventory/`: player-specific state and orchestration, if needed.
- `ITEMS/Definitions/`: shared Resource-driven item IDs, icon/art, weight, stack limit, equipment compatibility, optional shape/bulk.
- `SYSTEMS/` or a dedicated shared inventory/container subsystem: authoritative storage rules, item stacks, transfers and inventory persistence. Choose the final owning location when implementing; do not bury authoritative rules inside UI or duplicate them per actor.
- `FEEDBACK/`: floating numbers, health bars on entities, shakes and occlusion fading; not the inventory screen.

## Future Avalonia toolbox

Items and container/backpack parameters must eventually be authorable **through Resources and artwork**, with no new per-item Godot scenes or C# scripts. Consider item weight, category, icons, stack limits, rarity, optional display shape, equipment attachments and optional container capacities as data. New inventory designs may reference the same backing item data.

## Decisions still open

- Whether to use strictly weight or combine weight with bulk.
- Whether stacks of logs have a practical quantity limit and how bundles are represented.
- Whether character equipment is part of the main screen or a separate tab.
- Whether overloaded movement affects walk/run speed, stamina, or both.
- Whether a player can physically drop a backpack to reduce carried load.
- Controls and appearance of the quick-belt/radial tool selector.
- Whether a few oversized special items should consume shaped compartments.

## Implementation order (later, not in this commit)

1. Define reusable Resource-driven item data and inventory/stack state, separate from visuals.
2. Build the minimal weight-and-stack handling and safe transfer logic.
3. Add the player backpack screen with sorting, inspection and equipped items.
4. Reuse view components for chest/loot screens.
5. Add optional encumbrance, quick-access belt and specialized physical compartments only after usability tests.

**Acceptance target:** Players can chop trees, collect many logs, store them as legible stacks/bundles and move them between backpack and chest rapidly, while the screen still looks like a physical adventurer's pack.
