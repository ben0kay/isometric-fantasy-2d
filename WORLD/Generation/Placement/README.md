# Seeded scenery placement
WorldSceneryPlacement runs once at world startup from ordered biome populations. It adapts debug bounded rejection sampling, uniform positive scale, polygon mirroring and loose circular grass clustering. No per-frame scatter, species-name branching, demo trail, liquid or two-biome dependencies.

The supplied world seed drives positions, scale, variants and mirroring. Config bounds, spawn clearance, edge margin, retry limit and extra spacing constrain placement. Content definitions provide conservative circular spacing and artwork bounds. Impossible placements are skipped with totals/warnings, never retried indefinitely. Built-in content footprints fit their spacing circles; future content must supply appropriate conservative radii.

This is startup prop placement on a rectangular ground area, not macro terrain generation, connectivity checking, chunk streaming or saving. Extremely dense custom configurations can obstruct walking routes. Change data and reload to regenerate.
