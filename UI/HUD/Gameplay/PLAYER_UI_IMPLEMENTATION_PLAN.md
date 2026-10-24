# Player HUD, Hotbar & Inventory UI — Implementation Plan

> **Status: UI0 AUDIT COMPLETE; UI1–UI5 PLANNED — 24 October 2026. Documentation only.** No UI scenes, C# gameplay logic, input bindings or inventory systems are implemented by this note. Read root README.md, CodePreferences.md, UI/README.md, UI/HUD/Gameplay/README.md, UI/Inventory/PLAYER_INVENTORY.md, ENTITIES/Player/Input/README.md, ENTITIES/Player/Inventory/README.md and UI/Shared/Buttons/README.md before each phase. Confirm actual code and scenes at HEAD rather than relying only on READMEs. User approval required per phase.

## Phase overview

| Phase | Objective | Suggested assistant | Status |
| --- | --- | --- | --- |
| **UI0** | Audit PlayerInput, current world UI, button factory, item/inventory foundations and input conflicts | Regular Chat | **Complete: source audit** |
| **UI1** | Add shared gameplay HUD root and visual nine-slot bottom hotbar, initially placeholder data | Codex | Planned |
| **UI2** | Add inventory open/close action through PlayerInput and a non-pausing inventory overlay | Codex | Planned |
| **UI3** | Connect hotbar/inventory to authoritative item state and selection, after item contracts exist | Codex | Planned |
| **UI4** | Polish responsive organic styling, tooltips, focus and interaction behavior | Codex | Planned |
| **UI5** | Optional status/buff rail, top-left toasts and top-center alerts | Regular Chat plan → Codex | Planned |

## Existing project decisions to preserve

- UI/HUD/Gameplay/ is the owner of gameplay HUD presentation. UI/Inventory/ owns backpack, equipment and container views. ENTITIES/Player/Inventory/ owns player-specific orchestration; canonical item data and shared inventory/transfer rules must live outside UI.
- UI/Inventory/PLAYER_INVENTORY.md proposes a tactile travel-backpack appearance, weight-based capacity, automatic stacks/bundles for gathered resources, individual equipment, soft encumbrance and future container reuse. **Do not silently replace this vision with mandatory inventory Tetris.**
- That earlier note preferred a contextual quick-belt/radial selector rather than a permanent nine-slot bar. **This newer user direction supersedes it for the first HUD:** build a compact bottom hotbar, with nine slots as an initial adjustable design suggestion, not a locked gameplay limit.
- PlayerInput owns player control mappings and emits intent. Current temporary keys **1/2 select starter weapons and 3 toggles shield**; these conflict with hotbar number keys and must be audited/migrated deliberately. RMB currently controls hop/guard and later water floating, so avoid binding it to inventory UI without an explicit input mode.
- UIButtonFactory.Create()/Apply() and existing UIButtonStyle Resource are mandatory for **all actual Godot Buttons inside the inventory** (including close, sort, tabs and actions); use factory variants rather than hand-authoring conflicting Button theme overrides. Noninteractive item slots may be Controls, not fake Buttons.
- Existing UI folders are mostly planning only; do not claim the item/inventory backend is ready without code inspection.

## Visual direction

**Sleek, grounded, slightly organic fantasy** rather than chunky square RPG panels or high-tech glass. Use dark desaturated moss/charcoal, subtle leather/wood/bronze details, restrained warm accents, rounded or gently asymmetric frames and clear selected-slot emphasis. Avoid costly full-screen blur or shader effects for a UI that stays visible during combat.

**Bottom hotbar:** compact, centered near the bottom safe margin; suggest nine slots, adjustable by configuration. Each slot can show item icon, quantity, key label, cooldown/disabled state and selection border when the corresponding gameplay state exists. Do not invent fake item counts. Hotbar remains visible while the inventory is closed and may stay visible beneath an open inventory, with explicit input suppression where necessary.

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

## UI0 audit results — completed 24 October 2026

**Scope checked against current source:** \`project.godot\`, \`ENTITIES/Player/Input/PlayerInput.cs\`, \`ENTITIES/Player/Movement/PlayerMovement.cs\`, \`ENTITIES/Player/Equipment/EquippedTool.cs\`, \`ENTITIES/Player/Player.tscn\`, \`SCENES/World/World.tscn\`, \`WORLD/Runtime/WorldScene.cs\`, \`UI/Shared/Buttons/UIButtonFactory.cs\`, \`UIButtonStyle.cs\`, \`DefaultButtonStyle.tres\`, \`ITEMS/ItemMasterConfig.cs\`, existing UI/player inventory notes and menu styles. Source audit only; no editor/runtime validation.

| Area | Confirmed current behavior | Required UI1–UI3 handoff |
| --- | --- | --- |
| Input | One PlayerInput child on Player.tscn. Unhandled input emits combat mouse and starter-selection signals; raw _Input handles right-mouse/shift releases. | Add an InventoryToggleRequested intent in PlayerInput rather than direct raw-key polling in UI. |
| Bindings | project.godot has arrow movement actions, no inventory InputMap action. No current I-key handling in audited PlayerInput. | Create named player_inventory InputMap action, default I. Ensure I can close panel even if a button has keyboard focus. |
| Number keys | Keys 1/2/3 emit StarterSelectionRequested; EquippedTool switches axe/sword and toggles shield. Selection is temporary and can reject during attack/recovery. | UI1 displays hotbar only; do not steal number keys yet. UI3 migrates to 1–9 hotbar selection with intentional axe/sword/shield compatibility. |
| RMB | Right mouse press currently hop or guard, release passes through _Input even when UI consumes it. Future water plans also use RMB for flotation. | No inventory RMB behavior assumed; open inventory safely releases active guard/sprint and avoids combat input leaks. |
| Movement | PlayerMovement physics reads PlayerInput.ReadMovement, which uses Input.GetVector directly, independent of Control input consumption. | UI2 must gate movement/combat/shift/dodge explicitly when inventory is open. SceneTree keeps running; do not rely on MouseFilter alone. |
| Scene | World.tscn has separate TerrainRoot, SystemsRoot, DebugUIRoot and WorldActorsRoot with persistent Player; WorldScene caches Player on EnterTree. | Add one screen-space GameplayUIRoot CanvasLayer under World, not under player/camera/streaming chunks. Connect once and unsubscribe on world exit. |
| Debug overlap | F9 environment CanvasLayer uses layer 20 and left panel around x=16–366 when open. | Gameplay HUD below debug layer. Top-left toast lane later avoids expanded F9; bottom-centered hotbar stays separate. |
| Factory | UIButtonFactory.Create/Apply implemented, with Primary/Secondary/Danger/Small variants; UIButtonStyle provides colours and radius. | Every inventory Button must use factory. Future organic refinements should use shared style resource/factory, not per-view duplicated styling. |
| Items | ITEMS/ItemMasterConfig.cs empty; player Inventory folder documentation only. Combat EquippedItemDefinition is not general backpack/stack storage. | UI1/UI2 use honest empty slots/equipment placeholders. UI3 requires authoritative item stacks, equipment and save integration. |
| Layout | project.godot 1920×1080 canvas_items, expand aspect. | Anchor responsive bottom hotbar; test viewport resizes, smaller windows, safe margins. |
| Menus | MenuNavigation changes scenes and clears pause; current world pause/Esc arbitration not fully established by this audit. | UI2 priority recommendation: I toggles, Esc closes an open inventory before other menu behavior; inspect existing pause/session hooks when implementing. |

### Decisions carried into UI1/UI2

- Initial hotbar is **nine visual slots**, configurable; these are not fake usable items and do not change weapon controls in UI1.
- Inventory is a **non-pausing overlay**; enemies, physics, world and weather continue. Recommend freezing *player commands* while it is open, rather than freezing the world.
- Add input suppression at the central PlayerInput/PlayerMovement boundary so held arrows cannot move the player through the overlay. Suppress combat presses, clear active guard/sprint/dodge state safely, and keep UI toggle functioning while focused.
- Preserve C2.5 right-mouse release handling; future water flotation bindings remain separate.
- Add one HUD canvas owned by World with an embedded independent inventory view; no UI-owned inventory storage, duplicated HUD, or extra idle _Process loops.
- Use UIButtonFactory for inventory buttons; design rounded organic frames through passive controls/resources. Defer interactive item assignments/drag-and-drop until UI3.
- Validate Godot import/build, 1080p + smaller windows, I/Esc + focus, held keys/mouse, world still running, F9/F7 overlap, unload/reload lifecycle, idle and active frame cost in UI1/UI2.

**UI0 validation:** completed documentation and source inspection only. No gameplay/UI files edited and no Godot, .NET or graphical tests run. UI1 and UI2 remain separately authorized implementation phases.

### UI1 — bottom hotbar foundation
**Objective:** render a polished, resolution-aware nine-slot hotbar within one persistent gameplay HUD CanvasLayer.
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

1. **Hotbar slots:** use nine compact slots for now, Resource-configurable so the count can change later.
2. **Inventory toggle:** suggest I, owned by PlayerInput; confirm existing key conflicts first.
3. **World remains unpaused:** always; separately decide whether player movement/combat continues while inventory is open. Recommend blocking combat input over interactive UI, not stopping the world.
4. **No fake inventory backend:** UI1/UI2 may use empty placeholder slots; UI3 only after real item/stack ownership is defined.
5. **Inventory visual identity:** leather/moss/bronze, rounded and slightly organic; retain earlier physical backpack/weight design.
6. **Button factory:** every actual inventory Button uses UI/Shared/Buttons/UIButtonFactory.
7. **Notifications:** reserve distinct top-left and top-center lanes, but defer their runtime implementation until UI5.

**Handoff:** documentation only; do not automatically implement UI0–UI5. Each approved implementation is one GitHub commit with actual tests, deferred local checks and performance activation/shutdown notes.
