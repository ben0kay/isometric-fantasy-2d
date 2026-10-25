# WORLD/Content/Vegetation/Plants

Oak Forest plant source artwork and per-plant data definitions.

## Current state — PL2 asset staging

- Eight source PNGs have been moved from the repository root to `Sprites/plant_oak_001.png` through `Sprites/plant_oak_008.png`.
- Eight matching `Definitions/plant_oak_001.tres` through `plant_oak_008.tres` have been created with **distinct stable content IDs**, a reference to their corresponding PNG, and a one-element `metadata/visual_variants` array.
- These files are currently **staged SceneryDefinition resources**, not production-spawnable plant definitions. The PL1 `VegetationDefinition`/shared `Plant.tscn` runtime code has not been verified on the GitHub main branch. Until it is available, the PNG lists are held in Resource metadata (not guessed missing C# export properties).
- Once PL1 is present, adapt the eight `.tres` files to its exact exported visual-variant properties, preserving IDs, filenames and artwork links. Then perform the remaining PL2 biome population registration and PL3 world/streaming validation.
- **Nothing in OakForest/Biome.tres or PinkMeadow/Biome.tres has been changed.** Trees, grass and rocks are untouched. No harvesting or drops are enabled.

See [PLANTS_IMPLEMENTATION_PLAN.md](PLANTS_IMPLEMENTATION_PLAN.md) for the long-term contract: one shared plant scene, one .tres per distinct plant type, one or more PNG/atlas variants inside each definition, and explicit allowed definitions per biome.
