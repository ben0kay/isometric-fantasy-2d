# BC2 and standalone PreviewMap — implementation / local handoff

**Implemented, awaiting native/local validation.** This job adds the metadata-only `DEBUG/PreviewMap/PreviewMap.tscn` first, then climate-aware biome placement in the same production pipeline. BC3 weather, BC4 50× area, BC5 region selector and BC6 zoom/pan remain planned.

## Behavior and ownership

- `BiomePlacementClimate` is an optional shared Resource on `WorldBiomeDefinition.PlacementClimate`. It is separate from the existing Climate baseline. Null affinity is neutral; null world climate retains the old geography-only generator.
- Authored preferred Celsius/RH ranges and Gaussian falloff determine soft suitability (floor 0.001), multiplied by the existing weight and water suitability. Optional hard range flags never relax. `EnchantedOnly` and `AllowEnchantedException` opt into intentional thematic eligibility; the default preset still disables its enchanted province.
- Tundra range -100..6°C is hard. Desert 22..100°C and 0..55% RH are hard. Oak -2..24°C / 35..85% RH and Pink 5..26°C / 35..85% RH are soft. Thus cold/hot proxies group in appropriate areas, while one or two soft-affinity forest definitions still cover eligible land. Soft profiles can occasionally appear outside their preferred range; they are not hard climate tags.
- `ClimateBiomeRules` samples the canonical frozen field once per cell and caches weights during generation. `GeographyBiomeRules` composes climate with existing hard hydrology. Site choice, every growth edge and disconnected-component fallback use the same eligibility. Complete land coverage, four-connected regions and ocean non-land cells retain the existing growth algorithm. Unsupported hard requirements fail with cell coordinates, never invent a biome or erase water.
- Temporary cache: cells × enabled biome count × 4 bytes (~0.9 MB for 58102 cells/four biomes). Explicit guard is 16M entries (~64 MiB); released after map generation. No new runtime controller, per-object climate polling or chunk work.
- Default dimensions remain 142304×106728, 512-unit macro/gameplay cells and existing local streaming radii. This is not BC4.

## Frozen compatibility

Changed biome assignment uses **macro generator 4 / blueprint schema 11**. Climate remains field version 1, geography version 2, and the current local scenery generator remains unchanged. New World creation stamps schema 11 only when climate is supplied. SAVE validates schema/generator pairing and restores saved cell IDs plus frozen climate; old generator 1–3 / schemas 4–10 remain recognized. Exact content fingerprints still apply; authored affinity additions do not grant arbitrary migration of older campaigns. No existing campaign is rerolled. Use F6/new campaign for these generation changes.

## Shared viewer and parity

`MacroMapView` owns three read-only coarse layers used by both F7 and the preview scene. Neither host duplicates sampling/palette/raster code. No raster is allocated until the viewer is visible; unfinished work pauses while hidden and releases on rebind/unload. Macro generation itself remains synchronous; its measured planning time is distinct from staged raster preparation. There is no preview-worker concurrency or background chunk generator.

`WorldLiquidFootprint` extracts the existing fixed-pond polygon and bank-inclusive bounds without changing harmonic constants/order. Runtime uses the same helper. The preview reads the canonical pond config and uses the same world-space bounds as the stock scene, so the same seed/settings/exclusions use the same ocean/biome plan without loading a liquid Node.

## Validation

Performed: source/diff review, Resource path/ID checks, preview scene dependency review, whitespace checks. **Godot and .NET are unavailable here; build/import/runtime/save/UI/performance checks are deferred.** No native pass or measured FPS improvement claimed.

Suggested short local pass:

1. `dotnet build`; import with Godot .NET.
2. Optional targeted check: `godot --headless --path . res://TESTS/Generation/ValidateClimateBiomes.tscn`. Expected `BC2 CLIMATE BIOMES PASS`, exit 0. Checks all land against hard eligibility, representative Tundra/Desert presence, deterministic reordering, schema-11 JSON/frozen restore, single soft-biome coverage and explicit impossible-hard rejection.
3. F6 `DEBUG/PreviewMap/PreviewMap.tscn`. Same seed → same map hash; different seeds vary shapes. Biomes/Climate/Water agree; no World, chunks or effects beneath the root. Tundra/Desert stay inside hard limits; Oak/Pink fill temperate regions and sparse-content gaps. Hide/close or regenerate while a raster is incomplete; no stale work remains.
4. F6 normal World with that fixed seed; compare F7 map/water/climate hashes and protected start. New Campaign → save → Continue should retain schema-11 maps and exact player location. Existing water integration fixtures now recognize schema 11 and clear climate when explicitly constructing legacy snapshots. Geography-only regression keeps Authoring null to preserve its original version-3 scope.

BC0/BC1 audit remains historical evidence; this document owns the BC2 continuation. Production changes and preview are one job/commit on `dev`, with native acceptance pending.
