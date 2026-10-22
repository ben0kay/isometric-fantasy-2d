# Seeded scenery placement
WorldSceneryPlacement runs once at world startup from ordered biome populations. It adapts debug bounded rejection sampling, uniform positive scale, polygon mirroring and loose circular grass clustering. No per-frame scatter, species-name branching, demo trail or liquid dependencies.

The supplied world seed drives positions, scale, variants and mirroring. Config bounds, spawn clearance, edge margin, retry limit and extra spacing constrain placement. Content definitions provide conservative circular spacing and artwork bounds. Impossible placements are skipped with totals/warnings, never retried indefinitely. Built-in content footprints fit their spacing circles; future content must supply appropriate conservative radii.

This is startup prop placement on a rectangular ground area, not macro terrain generation, connectivity checking, chunk streaming or saving. Extremely dense custom configurations can obstruct walking routes. Change data and reload to regenerate.

## Adjacent biome composition
Generation processes primary then optional secondary populations using one seeded RNG and one shared occupancy cache. Sampling is restricted to conservative side rectangles and membership is rejected against AdjacentBiomeLayout's actual curve. Scenery metadata records biome/content IDs for inspection. Count, variants, scale and sharing stay in definitions; neither Oak nor Pink Meadow is hardcoded into the placer.

AvoidBiomeTransition is content data: trees/grass reserve the full blend plus sprite extent and conservative curve slope allowance. Shared rocks can occupy the transition, with ownership determined by their centre. Bounded retries may place fewer decorative tufts. Same seed + same layout/definitions/settings reproduces the same arrangement; changing composition changes old seed outputs.


## Resource-driven trees

TreeDefinition populations use VisualVariants and the one Shared/Tree.tscn. Seeded selection precedes fit checks; selected variant bounds and conservative trunk radius drive exclusion/occupancy. Configuration, mirrored root anchoring and collision reflection happen before AddChild, so downstream caches discover final sprites and components. Non-tree SceneryDefinition populations still use their existing PackedScene Variants.

## P3 streamed placement
ChunkSceneryData now supplies coordinate-keyed, density-based local candidate data with cross-chunk priority thinning. The stream materializes canonical tree/scenery templates and applies P2/water exclusions. This original WorldSceneryPlacement startup scatter is retained only for compatibility preview mode. See WORLD/Runtime/Streaming/README.md for density semantics and limits.
