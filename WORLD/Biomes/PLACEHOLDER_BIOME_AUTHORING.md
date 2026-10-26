# Placeholder biome authoring — finite-world macro/F7 experiments

BC2 is now implemented: the default Tundra/Desert/Oak/Pink definitions have PlacementClimate Resources and group using the frozen macro climate. This guide’s globally weighted observations describe the pre-BC2 baseline. Open DEBUG/PreviewMap/PreviewMap.tscn for the current metadata-only preview; BC3 regional weather remains pending.


**26 October update:** Snowy Tundra and Desert ground-only placeholders now join Oak/Pink in the normal finite World biome collection (F6/F7 or new campaign). No populations or new weather logic; climate placement remains BC1/BC2. Existing campaigns are not migrated by this addition. World Author ocean placement is broadly middle-biased with seeded offsets, not fixed-centred. Planned North/Mid/South starts must satisfy configurable clearance from major POI footprints as well as water/obstacle/access safety; see the [World Author plan](../Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md).


> **Planning/authoring guide, not an implemented climate clustering feature.** As of 26 October 2026, Oak Forest and Pink Meadow are the developed biomes; Snowy Tundra and Desert are registered ground-only placeholders. BP0/BP1 authored climate profiles exist but **do not influence where biome regions spawn**. The current P0/P5 generator selects from the world Biomes array by weight/target region diameter plus water/geography suitability. Read [BC0–BC7](Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) and the [World Author plan](../Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) before attempting cold/enchanted climate clustering, geographic presets, 50× expansion or regional starting locations.

## Fastest no-new-code preview in F6 World / F7

1. **Author a separate biome Resource per distinct test type** under `WORLD/Biomes/Definitions/<ExampleName>/Biome.tres` using Godot's Resource editor. It may **reference existing Oak/Pink ground/scenery definitions** for a layout prototype; no new Tree.tscn, actor, ore, harvest definition or per-biome script is needed. Reusing existing canonical Resources is intentional. Do not copy an existing `uid://` blindly into a newly duplicated file; let Godot assign the new Resource identity.
2. Set a **unique stable `Id`** (e.g. `biome.preview_frost_forest`), `DisplayName`, and visually distinct **`Colour`**. F7 uses Colour for the map swatch; it also influences the current base-ground tone, but **a tinted existing asset is not genuine mushroom/pine artwork**. Use `EnabledForGeneration=true`, nonzero `GenerationWeight` and useful `MinimumRegionDiameter`/`MaximumRegionDiameter` (in world units). Biomes with zero weight are not selected; sizes are targets rather than guaranteed exact footprints.
3. Keep `GroundStack` valid and reuse canonical `BiomePopulation` entries from existing scenery as needed; empty populations can make a map-only preview, **not a representative playable environment**. Optional `Climate` can define Celsius/%RH **baseline only**; it will not group biomes or affect snow today. Leave `EntitySpawns`/`ResourceSpawns` null or disabled/empty. Do not clone unnecessary standalone profiles.
4. Add each test `Biome.tres` to `WORLD/Generation/World/DefaultWorldGeneration.tres → Biomes` alongside Oak/Pink. This is the **actual default finite-world collection**; the old `OakAndMeadow.tres` layout is for the compatibility preview, not the current streamed World.
5. Open `SCENES/World/World.tscn`, run **F6** for a disposable session and press **F7** to view the entire macro map. Try the same fixed seed to compare weight/region-size changes if your debug controls provide a seed override. New Campaign uses the same configured biome collection, but **do not use an existing save for experiments**.

## Suggested placeholder palette (examples only — no files created)

| Optional test biome | F7 identity colour | Illustrative climate baseline | Future zone affinity (BC2, **not available today**) |
| --- | --- | --- | --- |
| `preview.frost_forest` | Pale icy blue | -8°C / 70% RH | Cold/highland |
| `preview.tundra` | Muted blue-gray | -15°C / 55% RH | Cold/highland |
| `preview.fungal_glade` | Rich purple | 12°C / 88% RH | Enchanted/humid |
| `preview.azure_meadow` | Teal/cyan | 18°C / 60% RH | Enchanted/temperate |
| `preview.ashen_reach` | Deep rusty red | 32°C / 15% RH | Hot/volcanic |

These are **layout mockups** and not real new biomes, permanent balance values, or new art. Their baseline temperature/RH won't affect generation before BC2, and BC1's climate-zone preview alone won't change biome site selection. Choose a handful of readable colours and test how well F7 communicates the composition of the large finite map.

**New World Author scenario for later BC1/BC2:** set a directed **CENTRAL OCEAN**, **FAR NORTH COLD**, **MIDDLE TEMPERATE**, **FAR SOUTH HOT**, then place frost/tundra placeholder types preferentially in northern climate provinces, cyan/purple types in deliberately authored enchanted areas, and Oak/Pink-like types in transitional middle climates. Biome seasonal appearance should be a **result** of regional temperature and later weather, not a simplistic north-only flag. Without BC2, these illustrative temperature labels are inert.

**50× area and spawn choices are later BC4/BC5:** do not multiply today's `DefaultWorldGeneration.tres.Bounds` immediately and assume it works. Current macro builder has `MaximumCells=262144` default and synchronous full-map generation; world-wide preview is one coarse image, while detailed 512-unit chunks are already streamed around player. Planned North/Middle/South startup selects **safe land** from the same seeded canonical region map and should never force-generate scenery between there and an unvisited original centre; [details](../Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md).

**BC6 F7:** zoom/pan and optional progressive **map-only** all-world preview with progress/cancel. It is a debugger for the canonical seeded plan, **not** a toggle to instantiate the whole detailed World.

## What to expect now vs later

- **Now:** F7 shows more colourful globally weighted connected regions, sized approximately by `Minimum/MaximumRegionDiameter`, with current ocean/lakes/rivers. **Don't expect all cold proxies to cluster or the enchanted ones to share a continent** from the temperature values alone.
- **After BC1:** F7 will be able to show the *new separately generated climate provinces*, even before biome regions consume them.
- **After BC2:** compatible placeholders should cluster **inside/near their climate provinces** based on authored affinity plus existing generation weights and sizes, including magic exceptions. With one or two real definitions, normal world coverage must still work without fabricated tundra/fantasy content.
- **After BC3:** actual regional rain/snow/settled cover becomes position-aware and can display warm/cold borders in one camera. This is separate from F7 biome map clustering and SE4 calendar.

## SAVE and performance guardrails

Creating or linking a new biome Resource changes the authored-content dependency graph and potentially generation/blueprint identity. **BP1's narrow content-fingerprint bridge does not mean arbitrary new biomes migrate old saved campaigns.** Back up existing saves, use a disposable F6 World or a fresh separate test campaign, and never silently reroll a saved world. New-wave climate changes need explicit versioned frozen blueprints.

A few lightweight biome Resources and F7 map colours add no per-object climate simulation. Existing P3 still streams only nearby scenery. When this becomes real, keep macro climate queries coarse/cached and weather managed by one shared runtime, not per-biome managers or per-tree polling. User-owned visual/performance validation is enough unless extended testing is requested.
