# Developer UI

F9 toggles the removable debug HUD as a whole. Fixed top-left **Environment**, **Combat** and **Ground Surfaces** category buttons open exclusive scrollable drawers; clicking the active category collapses it. Selecting another replaces the current drawer. `DebugCategoryMenu.AddCategory` supports future categories without another hotkey/overlay. Buttons/sliders do not capture movement-key focus.

The existing scene/root remains named `EnvironmentDebugPanel` for path compatibility; it hosts the category menu and the Environment body. Environment owns weather/time/lighting/snow controls, a collapsed **Tree fire** section and the existing right-side diagnostics. Those diagnostics hide in Combat/Ground or when Environment is collapsed. F7 remains the independent macro map. Fire's removable command owner remains `DebugUIRoot/FireDebugPanel`, but its widgets are hosted inside Environment instead of a separate overlay.

`Combat/CombatDebugPanel.cs` provides live ES3/separation controls, enemy melee/push/stagger/impact switches, a reset button and player/practice-actor counters. It restores original shared Resource values on unload; no ResourceSaver or campaign fields are used. Global masters affect all actors; ES3 sliders tune shared defaults, while explicitly authored species profiles retain their values.

Hidden/inactive drawer refresh callbacks stop; active widgets/counters refresh at 4 Hz. Environment effects, combat simulation and explicitly enabled C# tracing continue independently of HUD visibility. Fire performs receiver scans only when a button is pressed. Production code has no DEBUG dependency.

See [ES3/HUD validation](../../ENTITIES/Shared/Movement/ES3_Validation.md). `DEBUG/TopDownTest` remains an independent experiment.

[Ground/GroundSurfaceDebugPanel.cs](Ground/GroundSurfaceDebugPanel.cs) provides GS4 session-only base/layer comparisons through the production GroundSurfacePreview API. No Resource edits or texture rebuilding; hidden panels sleep. Coverage/opacity multipliers are global. Existing Oak/Pink have no optional overlays; test both layers with GroundLayersPractice.tscn. See [combined GS0–GS4 checklist](../../WORLD/Terrain/Ground/Surfaces/GS4_Validation.md).
