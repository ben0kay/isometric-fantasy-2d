# WORLD/Generation

Reusable world-generation **algorithms**, not an asset dump. Agreed new-world pipeline: required constraints → major geography/terrain/water → major POI/access reservations → biome assignment → local terrain/connections, scenery and populations. Continue restores frozen outcomes rather than rerunning this pipeline. See [the authoritative P5 plan](../Generation_Implementation.md). Use seeds and placement rules for deterministic revisit behaviour.

**Implemented:** `World/` owns the P1 finite macro biome map and its configuration; `Placement/` owns the existing preview startup scatter. `Liquids/` provides opt-in frozen lake and smooth river planning, rendered through the shared liquid runtime. P5a/P5b now runs major water before constrained biomes through WorldGenerationPipeline and the immutable [geography contract](World/Geography/README.md). Real ocean/volcano/POI providers remain P5c/P5d. See [macro generation](World/README.md).
