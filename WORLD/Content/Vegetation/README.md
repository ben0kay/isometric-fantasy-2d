# WORLD/Content/Vegetation

Reusable plant-like world objects. Species or types own their artwork, variation lists, growth/harvest parameters and collision footprints. Biomes control eligible species and distribution.

Trees and grass are production content. [Plants](Plants/README.md) now have a reusable PL1 scene/data foundation; PL2 populations and PL3 production integration are implemented, awaiting native/Windows validation. Shared weather adapters live under Shared/.

## Rain-soaked tree foliage

Shared tree wet-leaf tint is implemented in [Shared/Wetness/](Shared/Wetness/README.md). This presentation layer composes on top of the same wind materials used by generic trees before snow/fire overlays; it is not a separate vegetation species or new gameplay/weather simulation. Wetness is controlled by the existing rain strength and WeatherProfile settings.
