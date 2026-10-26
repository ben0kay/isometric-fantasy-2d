# Overworld biome definitions

The **currently authored production biomes** are `OakForest/Biome.tres` and `PinkMeadow/Biome.tres`. Shared typed Resources live in [../Shared](../Shared/README.md); existing scenery and art should be referenced from `WORLD/Content`, **not duplicated into each biome**. The old "Oak only" status is superseded.

For **future layout-only placeholder biomes**, use [the F6/F7 authoring guide](../PLACEHOLDER_BIOME_AUTHORING.md). Give each Resource a unique ID and map colour, and add it to the default world's Biomes list. These can reuse existing ground and scenery until real fantasy biomes are authored. They will not be grouped by cold/enchanted climate until the separately planned [BC0–BC3](OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) work. BP0/BP1 climate baselines are implemented but inert; BP2/BP3 ore/enemy spawns are different tasks.
