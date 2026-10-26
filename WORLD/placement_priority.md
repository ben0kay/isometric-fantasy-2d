# World generation placement priority

**Canonical plan:** [Generation_Implementation.md](Generation_Implementation.md), authoritative geography-first continuation / P5a–P5e. This table describes the **full future target**, not only current execution. P2 reservations, P1 biomes, WG lakes/rivers, P5a/P5b geography-first water, P5c ocean/non-land, P3 streaming and P4 saves already exist. **Macro climate zones, biome climate affinity and regional snow do not yet exist.**

Generation planning and local scene construction are separate. Required fixed landmarks can reserve constraints early; their buildings load only near the player.

**Shared weather design:** region climate and biome compatibility live in generation/biome Resources. Future snow and rain **ask one shared environment resolver** for local suitability and coverage, instead of instancing snow systems inside each biome. BP1 authored Oak climate baseline only; current precipitation still affects its globally configured World presentation. Introduce no runtime changes or migrations during this planning pass.

| Order | Pass | Main rule |
| --- | --- | --- |
| 1 | Bounds, seed and required constraints | Protect start requirements and mandatory feature/landmark zones; resolve fixed-anchor conflicts. |
| 2 | Major geography | Oceans, volcanic regions, mountains/cliffs, major lakes and rivers take shape before biomes. |
| 2b (future) | Macro climate/thematic zones | Group wide cold/temperate/hot/magical provinces into contiguous areas guided by geography and optional large map anchors. Biomes will subsequently pick compatible regions; no random per-site Oak → Alpine → mushroom alternation. |
| 3 | Major POIs and critical access | Select/reserve temples, villages, ruins, strongholds and important cave approaches against geography; reserve crossing needs and pass biome/theme requirements forward. |
| 4 | Biome assignment | Fill eligible land around geography, POI and **future climate-zone** constraints, choosing eligible biome definitions/weights within regions. Never leave land unassigned; oceans are non-land. |
| 5 | Detailed terrain and connections | Finalize ground, roads, bridges/fords and cave links; instantiate POI content locally, validate required reachability. |
| 6 | Large scenery | Trees, large rocks and dense vegetation respect all earlier masks. |
| 7 | Small scenery and resources | Place suitable smaller props/resources in remaining space. |
| 8 | Visual finishing | Shore details, gravel, leaf litter and local transitions. |

Use a shared reservation map with placement permissions, not just a universal occupied flag. A path blocks solid trunks/rocks but can allow low grass; a POI blocks random scenery but permits its own authored decoration; water excludes ordinary land props while allowing suitable shoreline content. Use actual shapes and clearance distances for irregular features rather than reserving their entire bounding rectangles.

Lower-priority content retries elsewhere instead of moving higher-priority features. Cap retries; skip optional scenery when necessary and provide fallbacks for required landmarks. Validate that the starting area, required POIs and cave approaches remain reachable.

Each generator receives the same world layout, reservation map and world seed. Derive separate deterministic random streams for terrain, POIs, paths and vegetation so changing grass counts does not relocate villages. A new world can choose a random seed, while generation from that chosen seed stays reproducible.
