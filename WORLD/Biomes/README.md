# WORLD/Biomes

Each future biome will have a clear, central profile (e.g. `TemperateForest/Biome.tres`) that **references** world content and entity species, with relative weights/densities and placement constraints. Keep assets in `WORLD/Content/`, wildlife/enemy definitions in `ENTITIES/`. **Do not create one `.spawn.tres` per item per biome.** An eventual Avalonia toolbox should inspect/edit biome profiles and preview referenced sprites. No biome definitions exist yet.

> **Scaffolding only:** This folder currently contains documentation, not implemented world content or generation logic.
