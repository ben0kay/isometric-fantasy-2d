# World generation placement priority

The complete pipeline below remains staged. **P2 implemented:** WORLD/Generation/World/Features provides required/preferred polygon reservations, priorities and permission-aware queries. Actual terrain/POI/streamed scenery consumers remain later passes; the playable preview keeps its existing spawn/pond exclusions.

Reserve space for important features before filling the world with scenery. Generation order and placement priority are separate: a required location can reserve its footprint early and be built later.

| Order | Pass | Main rule |
| --- | --- | --- |
| 1 | World bounds, biome layout and required starting area | Establish constraints and reserve a safe, reachable start. |
| 2 | Major terrain, lakes, rivers and cliffs | Establish shorelines and travel barriers; respect required reservations. |
| 3 | Major POIs | Reserve suitable footprints and clear approaches for villages, ruins, camps, etc. |
| 4 | Cave entrances | Reserve the entrance and walkable space outside; entrances may belong to POIs. |
| 5 | Paths and crossings | Connect destinations around barriers; add permitted bridges or fords. |
| 6 | Large scenery | Place trees, large rocks and dense vegetation outside protected footprints/routes. |
| 7 | Small scenery and resources | Fill suitable remaining spaces with small rocks, bushes, grass and loose items. |
| 8 | Visual finishing | Add gravel, shoreline details and ground variation around actual placements. |

Use a shared reservation map with placement permissions, not just a universal occupied flag. A path blocks solid trunks/rocks but can allow low grass; a POI blocks random scenery but permits its own authored decoration; water excludes ordinary land props while allowing suitable shoreline content. Use actual shapes and clearance distances for irregular features rather than reserving their entire bounding rectangles.

Lower-priority content retries elsewhere instead of moving higher-priority features. Cap retries; skip optional scenery when necessary and provide fallbacks for required landmarks. Validate that the starting area, required POIs and cave approaches remain reachable.

Each generator receives the same world layout, reservation map and world seed. Derive separate deterministic random streams for terrain, POIs, paths and vegetation so changing grass counts does not relocate villages. A new world can choose a random seed, while generation from that chosen seed stays reproducible.
