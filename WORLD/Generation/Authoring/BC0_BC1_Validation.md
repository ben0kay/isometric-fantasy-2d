# BC0 audit / BC1 implementation — 26 October 2026

**BC0: implemented (audit/design). BC1: implemented, awaiting local validation.** No 50× resize, biome-affinity selection, regional weather, automatic calendar or campaign region selector is included.

## BC0 decisions from current sources

| Concern | Confirmed current owner / decision |
| --- | --- |
| Creation | WorldGenerationPipeline synchronously plans features, water and complete coarse biome grid. Preserve this at current size; never call it fully lazy. |
| Detailed world | WorldChunkStream retains nearby 512-unit detail and existing activation/unload policy. No distant scene materialization. |
| Author guidance | Optional WorldAuthorPreset on existing WorldGenerationMasterConfig. Water settings retain ocean count/size. Broad preferred normalized middle zone; location may fall back only under explicit preferred policy with warning. |
| Climate | Value-only analytical field, versioned seeded smooth normalized noise. No additional coarse array or per-frame climate simulation. Same sampling function for saved worlds, F7 and future BC2 consumers. |
| Stable hierarchy | Versioned domain seeds for climate/ocean/start. Future BC4 sectors must key global coordinates and explicit stage versions with shared halos/ownership; query order must not consume mutable global RNG. |
| Planning order | Non-start reservations → water → climate → existing biomes → safe protected middle anchor → nearby scenery. Existing POIs are neutral reservations; BC5 must incorporate real major-POI footprints/accessibility when those providers arrive. |
| Start policy | Broad Middle zone for BC1, land/shore/POI setbacks; exact frozen feature anchor. North/Mid/South player preference UI and category overrides remain BC5. Fail visibly on exhausted bounded search. |
| Save boundary | Blueprint 10 requires climate snapshot + author policy; underlying map generator stays 3 because BC1 does not change biome affinity algorithm. Legacy 4–9 recognized, never rerolled or backfilled with climate. Strict content fingerprints are preserved, no blanket migration. |
| Size/resolution budgets | Current 142304×106728, 512 macro cells / ~58102 cells, MaximumCells 262144, MaximumRegions 4096. Existing O(cells log cells) growth and O(regions²) sites remain scalability constraints. |
| BC4 50× target | ~1006000×755000, ~7.07× each axis. Candidate 2048 macro scale (~182k cells), coarse analytical climate and existing 512 detail. Must prove costs, precision, organic boundaries and save sizes; no constants-only resize. |
| Preview boundary | F7 reads frozen results, no planner calls or distant chunk loading. On-open batches now; BC6 expands to zoom/tiles/progress/cancel and measured profiler. |
| Timing | Existing 250ms macro target remains a target, not a measurement. BC1 F7 2ms soft row budget / max 8 rows; one row/texture upload can exceed it. BC4/6 must add actual per-stage instrumentation, cancellation and measured acceptance. |

## Brief checks performed here

- Reviewed authoring, feature/water/biome pipeline, production scene wiring, manifest restoration, save version gate and F7 ownership.
- Static Resource references/declarations and whitespace checks passed; no production reference to TESTS/DEBUG assets added.
- Godot and .NET are unavailable in this environment. **No native compilation, import, runtime, disk Continue, visual or performance pass is claimed.**

## Focused local handoff

1. `dotnet build` then import/build in the installed Godot .NET editor.
2. Optional focused check: `godot --headless --path . res://TESTS/Generation/ValidateWorldAuthor.tscn`. Expected `BC1 WORLD AUTHOR PASS`, exit 0. Covers repeated generation, climate queries/order, JSON freeze/preset isolation, hash rejection, start/shore/POI buffers and null-author legacy feature path.
3. F6 `SCENES/World/World.tscn`, F7 → Show climate. Cold north, warmer south, organically warped boundaries and offset ocean should appear. Biome view still shows globally weighted Oak/Pink/Tundra/Desert; northern tundra placement is BC2. Close/reopen while building; no map work while hidden. Cached toggles should be immediate.
4. New Campaign, save, Continue: exact position, water/biome/climate hashes unchanged. Edit authoring only in a disposable copy: strict content checks may refuse Continue; it must never reroll a saved map. Check schema-9 snapshots restore no climate when passed directly through restoration with their original compatible content.
5. Optional later user profiling: hidden/settled F7 callbacks absent, no whole-map work while walking, no extra chunks from climate toggle. Existing world-wide macro creation remains synchronous.

Existing water/ocean integration assertions now accept schema 9 or 10; native regression checks were not run. BC0/BC1 constitute one implementation job; use the single commit linked in the delivery response. No subsequent BC phase is authorized by this handoff.
