# WORLD/Generation

> **BC4 — 26 October 2026:** BC4 implements bounded coarse-global planning plus stateless local border refinement for the 50×-area preset. Site/frontier work, water queries, river windows and palette uploads now have large-world paths. Full macro creation is still synchronous; detailed scenery remains nearby-only. [Current handoff](Authoring/BC4_Validation.md).


> **PreviewMap / BC2 — 26 October 2026:** BC2 implemented, awaiting local validation. Frozen macro climate now guides biome choice and growth; DEBUG/PreviewMap and F7 share the metadata-only viewer. Detailed chunks remain nearby-only. Larger bounds and regional weather remain later work. [Current handoff](Authoring/BC2_Validation.md).


> **BC0/BC1 delivery — 26 October 2026:** BC0 audit complete; BC1 implemented, awaiting local validation: optional WorldAuthorPreset, frozen analytical climate, middle-biased ocean guidance, deferred safe start and on-demand F7 climate layer. Existing local P3 streaming and world size remain unchanged. Biome affinity/weather/50×/campaign selector remain later phases. [Audit / local handoff](Authoring/BC0_BC1_Validation.md).


Reusable world-generation **algorithms**, not an asset dump. Agreed **future** pipeline: constraints → major geography/terrain/water → **coherent climate/thematic region map (planned)** → major POI/access reservations → compatible biome assignment → local terrain/connections, scenery and populations. Continue restores frozen outcomes rather than rerunning this pipeline. See [the authoritative P5 plan](../Generation_Implementation.md). Use seeds and placement rules for deterministic revisit behaviour.

**Implemented:** `World/` owns the P1 finite macro biome map and its configuration; `Placement/` owns the existing preview startup scatter. `Liquids/` provides opt-in frozen lake and smooth river planning, rendered through the shared liquid runtime. P5a/P5b now runs major water before constrained biomes through WorldGenerationPipeline and the immutable [geography contract](World/Geography/README.md). P5c now adds a major ocean provider and non-land biome cells; volcanic and POI providers remain planned. See [macro generation](World/README.md).

**Not yet implemented:** climate-region field, grouped cold/enchanted territory placement and position-dependent snow/weather. BP0/BP1 authored biome climate Resources are configuration only; do not confuse them with a climate map or regional runtime. See [climate-biome design](Biomes/README.md) and [main P5 continuation](../Generation_Implementation.md).

**World Author planned:** Inspector-driven geography/climate presets (OCEAN MIDDLE / COLD NORTH / HOT SOUTH), 50× **finite-area** scaling with globally coherent low-cost geography + on-demand detailed chunks, north/middle/south starting-area selection, and F7 world preview with zoom/progress/read-only profiler. **Existing world does global coarse planning but only local detailed streaming.** See [World Author implementation plan](Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md); BC0–BC7 are planned, not implemented.
