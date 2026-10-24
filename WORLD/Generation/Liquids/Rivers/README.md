# River generation — WG3

`RiverPlanner` chooses deterministic source/destination sites, routes through a bounded coarse cost field, adds organic bends/variable widths, and validates one complete liquid footprint. `PlannedRiverPath` retains copied centerline/width values and explicit SpringPool → TerminalBasin direction. Rendering, wading, rain, navigation and chunk lifetime stay with the existing shared liquid runtime; there is no second river renderer/manager.

## Settings

Rivers are opt-in: enable `WaterGenerationMasterConfig.Enabled` and set `RiverCount` above zero. Existing lake settings stay in the same Resource. River controls are grouped there rather than creating another `.tres` profile.

| Setting | Meaning / bound |
| --- | --- |
| RiverCount | 0 default; up to 4. Requested lake+river slots cannot exceed 32. |
| MinimumRiverSpan / MaximumRiverSpan | Endpoint separation in world units, default 4000–8000; accepted minimum ≥1500 and maximum ≤12000. Detour length may be longer; routes exceeding twice maximum span reject. |
| MinimumRiverWidth / MaximumRiverWidth | Base full width, default 100–180; accepted 64–384. Smooth variation plus wider source/terminal pools; width never fades to zero. |
| RiverBendStrength | Bounded lateral displacement, default 100, accepted 0–256 world units. |
| RequireMultipleRiverBiomes | Optional different endpoint biome types. Ignored only when the map has one configured biome; impossible multi-biome requests warn/skip. The practice fixture enables it. |
| RiverRouteCellSize | Independent routing grid, default 512, accepted 256–1024 world units. This is not the streaming chunk size or shoreline resolution. At most 65536 route cells. |
| AttemptsPerRiver | 12 default, maximum 32 route attempts; endpoint selection also has a bounded candidate loop. |
| MaximumRiverSearchNodes | 8192 default, accepted 32–65536 A* expansions per attempt. |
| Separation | Existing water placement margin also applies to rivers/reservations/authored pond and other water bodies. |

Biome Hydrology `AllowFreshwaterRivers` is the hard permission, checked across the whole corridor (including its terminal pools). `RiverSuitability` softly increases routing cost in less suitable areas and weights endpoint acceptance. Zero suitability prevents endpoint selection but can still be crossed if hard permission allows it. Null profiles mean allowed/weight 1. Lake permissions remain separate.

## Algorithm and scope

1. Build a conservative clearance/suitability grid after P1/P2 and lake placement. Protect required reservations, finite edges, the fixed pond and already placed water.
2. Choose independently seeded `river.NNN` source/destination slots, then deterministic weighted A* with stable tie IDs and no diagonal corner cutting.
3. Resample the full route to at most 24 controls. Add low-frequency lateral bends and continuous positive widths. Build one simple corridor polygon (at most 58 vertices), including rounded, widened spring/terminal pools.
4. Validate its entire final outline against biome cells, reservations, water separation and self-intersection; invalid candidates reject as a whole. Optional requests that exhaust bounds warn/skip rather than drawing broken fragments or pretending no valid route exists anywhere.
5. Freeze exact outcomes and direction/width metadata into the normal WaterBodyPlan. Do not reroute on Continue or chunk visits.

There is no elevation/downhill simulation yet. A terminal basin here is a widened visible pool, not terrain excavation or a simulated catchment. First rivers deliberately avoid existing lakes; connected lake/outlet networks, tributaries and drainage are later work. Water appears using WG2's pond-style shader and remains wadeable; downstream flow/current physics is WG6, detailed banks/carving is WG4.

## Performance and compatibility

Planning runs once per new world, with bounded grid, attempts and search. No river Node, timer or per-river update loop is introduced. Cached polygon-edge rectangles reject dry chunks inside a winding river's large bounding box before fields are prepared. Resident visibility and point exclusion use the same bounded index. The shared renderer retains its existing chunk-only fields and lifecycle; constant offline metadata stays cheap. Exact footprint eligibility is limited to 4096 coarse biome cells per candidate; unsupported disk/grid scales fail clearly or oversized corridors skip. Startup remains synchronous; profile target hardware rather than assuming an FPS improvement.

Lake-only plans keep water version 1 and their existing hash byte layout. River plans use water version 2 and world blueprint schema 6, so older executables reject them. Schema 4/5 Continue retains its original geography and never gains a river. See [WG3 validation and local checks](WG3_Validation.md).

For immediate testing, open `TESTS/Water/RiverPractice.tscn`, press F6, then R to jump through five saved river sections. F7 shows the cyan saved route with source/terminal markers; F9 retains shared rain controls. Production defaults remain off.
