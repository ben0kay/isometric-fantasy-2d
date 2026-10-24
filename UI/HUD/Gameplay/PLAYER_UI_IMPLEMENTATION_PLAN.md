# Player HUD, Hotbar & Inventory UI — Implementation Plan

> **Status: PLANNED / documentation only — 24 October 2026.** No UI scenes, C# gameplay logic, input bindings or inventory systems are implemented by this note. Read root README.md, CodePreferences.md, UI/README.md, UI/HUD/Gameplay/README.md, UI/Inventory/PLAYER_INVENTORY.md, ENTITIES/Player/Input/README.md, ENTITIES/Player/Inventory/README.md and UI/Shared/Buttons/README.md before each phase. Confirm actual code and scenes at HEAD rather than relying only on READMEs. User approval required per phase.

## Phase overview

| Phase | Objective | Suggested assistant | Status |
| --- | --- | --- | --- |
| **UI0** | Audit PlayerInput, current world UI, button factory, item/inventory foundations and input conflicts | Regular Chat | Planned |
| **UI1** | Add shared gameplay HUD root and visual six-slot bottom hotbar, initially placeholder data | Codex | Planned |
| **UI2** | Add inventory open/close action through PlayerInput and a non-pausing inventory overlay | Codex | Planned |
| **UI3** | Connect hotbar/inventory to authoritative item state and selection, after item contracts exist | Codex | Planned |
| **UI4** | Polish responsive organic styling, tooltips, focus and interaction behavior | Codex | Planned |
| **UI5** | Optional status/buff rail, top-left toasts and top-center alerts | Regular Chat plan → Codex | Planned |

## Existing project decisions to preserve

- UI/HUD/Gameplay/ is the owner of gameplay HUD presentation. UI/Inventory/ owns backpack, equipment and container views. ENTITIES/Player/Inventory/ owns player-specific orchestration; canonical item data and shared inventory/transfer rules must live outside UI.
- UI/Inventory/PLAYER_INVENTORY.md proposes a tactile travel-backpack appearance, weight-based capacity, automatic stacks/bundles for gathered resources, individual equipment, soft encumbrance and future container reuse. **Do not silently replace this vision with mandatory inventory Tetris.**
- That earlier note preferred a contextual quick-belt/radial selector rather than a permanent nine-slot bar. **This newer user direction supersedes it for the first HUD:** build a compact bottom hotbar, with six slots as an initial adjustable design suggestion, not a locked gameplay limit.
- PlayerInput owns player control mappings and emits intent. Current temporary keys **1/2 select starter weapons and 3 toggles shield**; these conflict with hotbar number keys and must be audited/migrated deliberately. RMB currently controls hop/guard and later water floating, so avoid binding it to inventory UI without an explicit input mode.
- UIButtonFactory.Create()/Apply() and existing UIButtonStyle Resource are mandatory for **all actual Godot Buttons inside the inventory** (including close, sort, tabs and actions); use factory variants rather than hand-authoring conflicting Button theme overrides. Noninteractive item slots may be Controls, not fake Buttons.
- Existing UI folders are mostly planning only; do not claim the item/inventory backend is ready without code inspection.

## Visual direction

**Sleek, grounded, slightly organic fantasy** rather than chunky square RPG panels or high-tech glass. Use dark desaturated moss/charcoal, subtle leather/wood/bronze details, restrained warm accents, rounded or gently asymmetric frames and clear selected-slot emphasis. Avoid costly full-screen blur or shader effects for a UI that stays visible during combat.

**Bottom hotbar:** compact, centered near the bottom safe margin; suggest six slots, adjustable by configuration. Each slot can show item icon, quantity, key label, cooldown/disabled state and selection border when the corresponding gameplay state exists. Do not invent fake item counts. Hotbar remains visible while the inventory is closed and may stay visible beneath an open inventory, with explicit input suppression where necessary.

**Inventory:** non-pausing centered/side overlay with backpack contents, equipment area and capacity summary. The world remains visible and continues simulating; use a subtle dim/background panel only if it does not block visual awareness. Initially show honest placeholder or empty states, not fake persistent items. Later reuse item stack and slot widgets for chests and loot.

**Future peripheral HUD:** a compact vertical rail of buff/debuff/status icons on one screen side; independent toast lane entering from **top-left**; prominent alert lane at **top-center**. Keep their root containers and layout reservations separate so messages do not overlap inventory, hotbar or F9 debug controls. Toasts are transient low-priority feedback; alerts are higher-priority warnings and should not block movement.

## Proposed ownership (create only when phase requires)

```text
UI/
├── HUD/
│   └── Gameplay/
│       ├── README.md                    # EXISTS
│       ├── PLAYER_UI_IMPLEMENTATION_PLAN.md # THIS FILE
│       ├── GameplayHUD.tscn             # PROPOSED — one shared HUD canvas/root
│       ├── Hotbar/                      # PROPOSED — visual slots/selection
│       ├── StatusEffects/               # FUTURE — buff/debuff icon rail
│       └── Notifications/               # FUTURE — toast + alert views
├── Inventory/
│   ├── README.md                        # EXISTS
│   ├── PLAYER_INVENTORY.md              # EXISTS — detailed design vision
│   ├── PlayerInventoryView.tscn         # PROPOSED
│   ├── Components/                      # PROPOSED — reusable item/stack widgets
│   └── Containers/                      # EXISTS — future container views
└── Shared/
    └── Buttons/                         # EXISTS — UIButtonFactory + styles

ENTITIES/Player/Input/                    # EXISTING PlayerInput handles inventory intent
ENTITIES/Player/Inventory/                # Future authoritative player inventory adapter
ITEMS/                                    # Canonical item definitions, not UI
```

Do not put input polling or item authority into the HUD scenes. UI observes player/inventory state through signals/events and sends explicit selection/transfer requests to the owning gameplay system. Avoid a second UI inventory database.

## Detailed phases

### UI0 — audit and interaction contract
**Objective:** verify actual input implementation, current world UI/CanvasLayer composition, UIButtonFactory APIs, viewport anchors, item definitions and save ownership.
**Scope:** decide inventory input action (recommend **I**, but inspect existing bindings), how focus and pointer capture work, whether opening inventory blocks movement/attacks or only consumes UI-focused clicks, number-key migration from temporary weapon/shield controls, and hotbar backend readiness.
**Out of scope:** scenes or behavior changes.
**Performance:** identify shared signal wiring and hidden-UI sleep.
**Pass:** short documented mapping and no conflicting controls or invented item backend.
**Assistant:** Regular Chat.

### UI1 — bottom hotbar foundation
**Objective:** render a polished, resolution-aware six-slot hotbar within one persistent gameplay HUD CanvasLayer.
**Scope:** shared slot visual component, selected state, placeholders/empty icons, numeric labels, theme/resource-driven size/spacing/colour settings; proper world-scene hookup without duplicating HUDs on reload.
**Out of scope:** inventory item transfer, drag/drop, real equipment, replacing test weapon mappings before UI0 decisions.
**Preserve:** combat HUD/debug layers, F9 controls, world camera and mouse gameplay.
**Performance:** no per-slot _Process; only update on selection, inventory or configuration changes; shared materials/styles.
**Pass:** scales at 1080p and smaller windows, no clipping, no duplicate HUD on world reload, zero idle updates.
**Assistant:** Codex.

### UI2 — non-pausing inventory overlay and input
**Objective:** press inventory action emitted by PlayerInput to open/close the inventory while the world continues.
**Scope:** inventory panel and backpack/equipment placeholders, open/close handling, Esc policy consistent with existing pause/menu, factory-created Buttons, click/focus routing and MouseFilter behavior, optional overlay animation only while opening/closing.
**Out of scope:** backend storage, weight rules, actual items, pause-tree changes.
**Preserve:** world simulation, player input semantics and RMB jump/guard; decide whether player can move while inventory is open (recommend allow world to run but suppress combat/movement while UI has focus, subject to user preference).
**Performance:** instantiate once or lazily; hidden means dormant; animations stop when settled; no polling to detect visibility.
**Pass:** I toggles, UI buttons work, clicking inventory never attacks world, closing restores focus, weather/enemies continue updating, no accidental duplicate inventory instances.
**Assistant:** Codex.

### UI3 — authoritative inventory and hotbar binding
**Objective:** make UI reflect actual item definitions, stack counts and hotbar references when gameplay item storage is ready.
**Scope:** shared item/stack view-model contract, hotbar assignment/selection requests, optional quantity, icon and tooltip updates; equipment state from canonical owner. Migrate temporary number-key combat selection intentionally.
**Out of scope:** inventing a complete crafting/economy system, mandatory inventory Tetris, chests/looting implementation.
**Preserve:** existing weapon/shield combat behavior via compatible mappings and authoritative equipment ownership.
**Performance:** change-driven updates, no full item list rebuild every frame, reuse item widget instances.
**Pass:** selection changes real gameplay state, empty slots safe, item changes refresh UI once, save/load preserves authoritative inventory and assignments when persistence is implemented.
**Assistant:** Codex.

### UI4 — usability and visual polish
**Objective:** refine organic frames, hover/selected/pressed states, item tooltips, drag/drop readiness and screen layout.
**Scope:** responsive anchors/safe areas, input focus/navigation, accessibility contrast, optional subtle transitions, sorting/inspection buttons via shared factory.
**Out of scope:** extensive art pipeline, animated full-screen shaders or extra systems without item state.
**Performance:** animations run only on state changes; no blur/per-frame material churn.
**Pass:** readable at multiple resolutions, keyboard/mouse controls consistent, inventory doesn't obscure essential alert region, no idle draw churn.
**Assistant:** Codex with user visual review.

### UI5 — later status and notifications
**Objective:** add three independent presentation channels without entangling gameplay systems.
**Scope:** vertical status icon rail (buffs/debuffs/conditions), **top-left sliding toasts**, **top-center alerts**, event-driven shared notification service, priorities and coalescing/queue limits.
**Out of scope:** implementing buff effects, health mechanics or new gameplay rules in UI.
**Performance:** pooled/reused widgets, bounded visible counts and queue sizes, timers/animations only while messages active; dormant otherwise.
**Pass:** correct screen anchors and stacking, no overlap with F9 or inventory, repeated events bounded, off/idle zero updates.
**Assistant:** Regular Chat plan, Codex implementation.

## Key decisions for first implementation

1. **Hotbar slots:** suggest six compact slots now, Resource-configurable; avoid hardcoding nine.
2. **Inventory toggle:** suggest I, owned by PlayerInput; confirm existing key conflicts first.
3. **World remains unpaused:** always; separately decide whether player movement/combat continues while inventory is open. Recommend blocking combat input over interactive UI, not stopping the world.
4. **No fake inventory backend:** UI1/UI2 may use empty placeholder slots; UI3 only after real item/stack ownership is defined.
5. **Inventory visual identity:** leather/moss/bronze, rounded and slightly organic; retain earlier physical backpack/weight design.
6. **Button factory:** every actual inventory Button uses UI/Shared/Buttons/UIButtonFactory.
7. **Notifications:** reserve distinct top-left and top-center lanes, but defer their runtime implementation until UI5.

**Handoff:** documentation only; do not automatically implement UI0–UI5. Each approved implementation is one GitHub commit with actual tests, deferred local checks and performance activation/shutdown notes.
