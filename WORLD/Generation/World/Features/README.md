# Guided layout and reservations — P2

Implemented: seeded, required/preferred polygon footprints with placement priorities, permission-aware queries, independent random streams and frozen save-contract outcomes. `WorldGenerationMasterConfig.Layout` references `DefaultWorldLayout.tres`. The default preset contains **two inline Resources**, not another scene or duplicate biome collection:

- Required protected start: centred at normalized (0.5,0.5), 1600×1600 footprint plus 160-unit clearance. Allows low vegetation and paths, excludes unrelated solid scenery, water and destinations.
- Required SE test footprint: irregular six-point 9000×7000 outline plus 200-unit clearance, placed wholly inside normalized zone (0.6,0.6)–(0.95,0.95). It is a neutral reservation, **not** a fake ocean, volcano or extra biome.

F6 World → **F7** shows tinted reservations and white anchor crosses over the biome map. Tint includes clearance. The playable adjacent-biome scene remains unchanged; these reservations describe the future larger world. Its existing local spawn clearance/pond avoidance still operate separately. P3 must connect streamed placement to these queries and use StartingArea.Anchor for the new map's player arrival.

## Authoring contract

| Setting | Meaning |
| --- | --- |
| Id / DisplayName / Enabled | Stable feature instance identity and presentation. Enabled IDs must be unique within the layout. IDs are scoped to WorldId in future persistence. |
| Required | Failure aborts planning with feature/conflict context. Preferred valid-but-infeasible requests warn and skip. Malformed data always fails. |
| Priority | Higher first within required/preferred groups. Required requests are processed before all preferred ones; ties sort by ordinal Id. |
| IsStartingArea | Optional designation of the single required starting reservation. It is a macro anchor, not an instruction to relocate today's preview player. |
| Placement | AnywhereInZone samples bounded candidates; FixedAnchor honours the exact normalized anchor. Neither silently relocates a fixed request. |
| NormalizedZone / NormalizedAnchor | Fractions of finite bounds, origin-aware; +Y points down. **The complete footprint and clearance**, not only its centre, must fit the zone. |
| Size / MinimumScale / MaximumScale | World-unit dimensions multiplied by one seed-chosen scale per feature. Scale is selected once, not silently shrunk during retries. |
| NormalizedPolygon | A simple, possibly concave outline with 3–64 vertices in [-0.5,0.5]. No duplicate adjacent points, self-touch/intersection or zero area. The default is rectangular; custom data supplies irregular masks. |
| Clearance | Extra world-unit distance around actual polygon edges, including concave boundaries. |
| Kind / AllowedPlacements | Category mask for this feature and kinds allowed to overlap it. These express interactions, not an asset catalog or terrain type. |
| OverviewColour | Developer presentation; changing colour/name does not change the geometry hash. |

Layout limits: default 64 attempts per request and 128 requests; accepted configurable limits are 1–256 each. If a selected size cannot fit the zone, planning fails/skips immediately. Required conflicts report the blocking feature; no partial result is published to WorldScene. A required feature may still fail after bounded retries even if some untried valid placement exists; the error is explicit, never a claim that the entire feasible space was searched.

## Permissions and geometry

`WorldPlacementKind` flags are LowVegetation (1), SolidScenery (2), Path (4), Water (8), PointOfInterest (16), CaveEntrance (32), MajorTerrain (64). None allows nothing; All allows every supported category. For example, a path reservation can allow low grass while blocking solid trunks. An owner may explicitly supply its feature ID to place its own authored decorations; this is an internal ownership contract, not user authentication.

`WorldReservationMap.AllowsDisk(position, radius, kind, ownerFeatureId)` rejects out-of-world/invalid candidates and checks every intersecting reservation's permissions. Conservatively represent a small prop by its placement radius. Major requests use exact polygon-to-polygon intersections and edge distances. Bounding rectangles are an early rejection only: empty corners of concave footprints remain usable. When two feature footprints overlap, permissions must be **mutually** compatible; a later candidate cannot exclude an earlier feature's kind. Earlier footprints are never moved or overwritten.

All queries operate on the complete small feature plan without needing loaded chunks. The plan stores cloned world-coordinate outlines; source Resource edits and snapshot array edits cannot alter it. Queries are linear in the bounded feature count with cheap spatial rejection. A spatial index can be added if measured P3 workloads require it; no full-world scenery/physics nodes are created here.

## Ordering, determinism and save handoff

WorldScene plans features before building the random biome map, then publishes both results atomically. This pass reserves geography but **does not carve land, override biomes, render water, route paths, or place POI scenes**. All land still receives the P1 biome assignment. Future terrain passes must consult these outcomes before applying barriers/content, with explicit landmask/biome directives when those mechanics are approved.

A feature's stream derives from world seed, planner version, stream tag and stable feature ID using explicit integer math. Adding/reordering an unrelated request consumes no other feature's RNG and leaves the P1 biome stream/hash untouched. New higher-priority conflicts can intentionally change lower-priority placement. Renaming a stable ID intentionally changes its stream/identity; change only the display name for cosmetic renames.

`WorldFeaturePlan.Hash` covers seed/version/bounds and exact placed geometry/permissions. It is separate from the biome hash. `WorldBlueprintManifest` schema 2 adds planner version/hash and detached `WorldFeatureSnapshot` entries, including starting designation. This records placed outcomes for future SAVE rather than rolling requests again on Continue. Durable profile saves, restoration/migrations, chunk deltas and persistence files remain P4 work.

## Validation and limits

`TESTS/Generation/ValidateFeatureReservations.tscn` checks 16 seeds plus exact repeats, footprint/clearance fit, actual position variation, required conflicts, preferred bounded skips, priority and order, independent streams, concave empty corners, crossing outlines, edge clearance, category/owner permissions, bad authoring and feature snapshot JSON round-trip. P1 acceptance and unchanged-preview placement checks also pass. F7 toggling, panel size and the generated overview image are checked headlessly; the map image was inspected. Native graphical Windows/D3D12 remains a local check.

On this runner the initial 32 default feature-plan builds took 39 ms total including first-call costs. This is a small-plan observation, not a frame-rate guarantee. Generation and resource reads remain synchronous/main-thread; worker staging is later streaming work. The required start is protected from overlapping incompatible reservations, but **walkability/reachability through future terrain is not yet proven** because no such terrain/path network exists. No oceans, volcanoes, caves, encounter spawning or navigation changes are included.
