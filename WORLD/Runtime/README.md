# World runtime
WorldScene composes the configured single-biome map: selects/logs one seed, configures ground, spawn/camera bounds, creates four static boundary walls, then calls startup placement. No persistence, chunks or environment simulation yet.

After seeded placement, WorldScene initializes the optional EnvironmentRuntime node so its caches see complete production scenery. Optional developer UI binds later; neither world generation nor the environment reads panel state.
