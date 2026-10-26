# Macro generation overview

Optional `MacroMapDebugPanel` in the production World scene. Press **F7** after F6 to show/hide the finite biome blueprint, seed/version/hash, world identity, region counts and cell coverage. Colours come from biome Resources; dark borders distinguish region instances. This is an overview of the new map, not the player location or a streamed playable world.

Removing the panel leaves generation and all gameplay functional. Generation settings belong to `WORLD/Generation/World/DefaultWorldGeneration.tres`, referenced by WorldMasterConfig; biome data stays in canonical biome definitions. There are no generator settings owned by this UI.

## Planned BC1/BC6 F7 world atlas and profiler — no implementation yet

**Source audit:** current `MacroMapDebugPanel.BuildPanel` eagerly loops over **every macro cell** to draw an Image at World startup, even when hidden; it overlays water/features and renders a fit-only TextureRect. This is **not** the entire world's scene/chunks; it is rasterized coarse data. At the proposed **50× area** target this needs a redesign.

- **BC1:** simple read-only climate layer aligned to authoritative seed/blueprint and the new World Author cold-north/hot-south preset.
- **BC6:** responsive **fit-to-world** low-res thumbnail on F7 open, mouse-wheel zoom (cursor anchored), drag to pan, coordinate readout, scale display and reset; selectable overlays for **oceans/lakes/rivers, climate/temperature/humidity, biome regions, major features, player + proposed spawn region and optional chunk grid**.
- **Optional whole-world preview:** user explicitly starts an exhaustive **LOW-DETAIL geography/map-raster job**, staged and budgeted with **real percent/progress/cancel**. Map-only image tiles may be sampled in deterministic order with on-demand higher-resolution tiles near zoom; show current resolution, stage and timing. **Never load all gameplay chunks or instantiate distant trees/collision**.
- **Profiler view:** read-only stage timing for macro planning/Continue restoration, number of macro cells and climate/biome regions, raster/texture/cache memory estimates, generated map tiles, active P3 chunk count/queue and seed/hash/version. Poll nothing expensive when F7 is closed.
- **Boundary:** DEBUG F7 must never become the canonical planner or mutate a saved world; it consumes `WorldGenerationPipeline`/`WorldBlueprintManifest` data. No main-thread megaloop on World spawn just to prepare a hidden optional panel. Pure CPU off-thread work only where Godot thread-safety permits, texture creation main thread.

[Authoritative BC0–BC7 plan](../../../WORLD/Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) explains the 50× world, geographic invariants, regional spawn and phased acceptance.

P2: tinted polygons include feature clearance; white crosses mark anchors. The legend lists placed required/preferred features and warnings. Geometry and all permissions remain owned by WORLD/Generation/World/Features. No new biome or terrain is rendered by the overlays.
