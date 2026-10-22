# WORLD/Content/Vegetation

Reusable plant-like world objects. Species or types own their artwork, variation lists, growth/harvest parameters and collision footprints. Biomes control eligible species and distribution.

> **Scaffolding only:** This folder currently contains documentation, not implemented world content or generation logic.

## Rain-soaked tree foliage

Shared tree wet-leaf tint is implemented in [Shared/Wetness/](Shared/Wetness/README.md). This presentation layer composes on top of the same wind materials used by generic trees before snow/fire overlays; it is not a separate vegetation species or new gameplay/weather simulation. Wetness is controlled by the existing rain strength and WeatherProfile settings.
