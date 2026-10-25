# River generation — WG3

`RiverPlanner` chooses deterministic source/destination sites, routes through a bounded coarse cost field, adds organic bends/variable widths, and validates one complete liquid footprint. `PlannedRiverPath` retains copied centerline/width values and explicit SpringPool → TerminalBasin direction. Rendering, wading, rain, navigation and chunk lifetime stay with the existing shared liquid runtime; there is no second river renderer/manager.

## Settings

Rivers are opt-in: enable `WaterGenerationMasterConfig.Enabled` and set `RiverCount` above zero. Existing lake settings stay in the same Resource. River controls are grouped there rather than creating another `.tres` profile.

| Setting | Meaning / bound |
| --- | --- |
| RiverCount | 0 default; up to 4. Requested lake+river slots cannot exceed 32. |
| MinimumRiverSpan / MaximumRiverSpan | Endpoint separation in world units, default 6000–11000; accepted minimum ≥1500 and maximum ≤12000. Detour length may be longer; routes exceeding twice maximum span reject. |
| MinimumRiverWidth / MaximumRiverWidth | Base full width, default 100–180; accepted 64–384. Smooth variation plus wider source/terminal pools; width never fades to zero. |
| RiverBendStrength | Bounded lateral displacement, default 280, accepted 0–512 world units. |
| RiverMeanderLength | Broad S-bend wavelength, default 2400, accepted 1200–6000 world units. Independent of routing/chunk grid. |
| RiverBankIrregularity | Inspector-only bank variation, default 0.15, accepted 0–0.3 (fraction of local half-width). Independent 170–430-unit bank waves; zero removes small offsets while retaining curved meanders. |
| RequireMultipleRiverBiomes | Legacy biome-first API/tests only. New geography-first rivers ignore biome identities/counts, so this does not force a crossing or reroute them. |
| RiverRouteCellSize | Independent routing grid, default 512, accepted 256–1024 world units. This is not the streaming chunk size or shoreline resolution. At most 65536 route cells. |
| AttemptsPerRiver | 12 default, maximum 32 route attempts; endpoint selection also has a bounded candidate loop. |
| MaximumRiverSearchNodes | 8192 default, accepted 32–65536 A* expansions per attempt. |
| Separation | Existing water placement margin also applies to rivers/reservations/authored pond and other water bodies. |

In P5a/P5b production creation, biome Hydrology now constrains **biome assignment around established rivers**. `AllowFreshwaterRivers=false` prevents that biome from claiming touched coarse cells; `RiverSuitability` softly biases compatible site/growth selection. Zero suitability stays allowed if the hard flag permits it. Null profiles are permissive. The explicit legacy `WaterBodyPlanner.Build(config,map,features)` still applies the original corridor/endpoint checks for baseline tests; it is not used for new production worlds.

## Algorithm and scope

1. Build a conservative clearance grid after P2 reservations and lake placement, before P1 biome assignment. Protect required reservations, finite edges, the fixed pond and already placed water.
2. Choose independently seeded `river.NNN` source/destination slots, then deterministic weighted A* with stable tie IDs and no diagonal corner cutting.
3. Resample and soften the obstacle-avoidance route into coarse anchors, interpolate continuous cubic curves, then add broad distance-based S meanders. Sample the curve at roughly 32 world units (at most 1000 samples). Independently offset each bank with smaller distance-based waves; construct one validated corridor (at most 2048 vertices), including rounded widened terminal pools. Routing anchors never become visible straight bank segments.
4. Validate its entire final outline against biome cells, reservations, water separation and self-intersection; invalid candidates reject as a whole. Optional requests that exhaust bounds warn/skip rather than drawing broken fragments or pretending no valid route exists anywhere.
5. Freeze exact outcomes and direction/width metadata into the normal WaterBodyPlan. Do not reroute on Continue or chunk visits.

There is no elevation/downhill simulation yet. A terminal basin here is a widened visible pool, not terrain excavation or a simulated catchment. First rivers deliberately avoid existing lakes; connected lake/outlet networks, tributaries and drainage are later work. Water appears using WG2's pond-style shader and remains wadeable; downstream flow/current physics is WG6, detailed banks/carving is WG4.

## Performance and compatibility

Planning runs once per new world, with bounded grid, attempts and search. No river Node, timer or per-river update loop is introduced. Cached polygon-edge rectangles reject dry chunks inside a winding river's large bounding box before fields are prepared. Resident visibility and point exclusion use the same bounded index. The shared renderer retains its existing chunk-only fields and lifecycle; constant offline metadata stays cheap. Exact footprint eligibility is limited to 4096 coarse biome cells per candidate; unsupported disk/grid scales fail clearly or oversized corridors skip. Startup remains synchronous; profile target hardware rather than assuming an FPS improvement.

Lake-only plans keep water version 1 and their existing hash byte layout. New smooth river plans keep water version 3; new geography-first worlds use blueprint schema 8 / macro generator version 2, so older executables reject them. Previously saved version-2/schema-6 rivers retain their exact polygons and hashes. Schema 4/5 Continue retains its original geography and never gains a river. See [WG3 validation and local checks](WG3_Validation.md).

For immediate testing, open `TESTS/Water/RiverPractice.tscn`, press F6, then R to jump through five saved river sections. F7 shows the cyan saved route with source/terminal markers; F9 retains shared rain controls. Production defaults remain off.

Bank irregularity and longer default spans apply only to newly generated worlds (including RiverPractice). Saved polygons remain authoritative; no in-game sliders or additional runtime callbacks. Dense river geometry now has separate bounded limits; lake geometry retains its 64-vertex limit. WG4 terrain/shore blending remains separate.

Chunk distance fields collect only edges within the 256-unit distance saturation radius and compute inside/outside crossings once per row. They retain the existing world-aligned texels, halo and eight-row yields. Point queries cache edge vectors and reject farther edge bounds before distance calculations. Geometry validation/planning is bounded but more expensive than the original coarse corridor; target-PC startup, navigation and streaming profiling remain necessary. No river-shape work runs per frame.
