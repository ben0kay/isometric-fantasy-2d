# SE2 — composition, lifecycle and seasonal save compatibility

**Implementation complete; native Godot/Windows validation and measured performance acceptance pending.** Source compatibility and translated software-GL checks passed. No new season controls, automatic calendar, save schema or fire visual feature.

## Changes and boundaries

- Existing shared season atlas continues through wet/snow/active-fire material copies. Expanded tests cover changing wetness, snow removal mid-burn, current-season chunk binding, stable timing on return and cache teardown.
- FireErosionAdapter now reference-counts its composed shader cache. Last active user releases the entry on completion/reset, weather-material replacement or cancellation; there is no permanent process-wide retention after worlds unload.
- TreeFireAppearance completion now always stops processing, removes temporary overlays and emits its appearance notification, even without a BurntTexture. Oak uses its existing charred sprite. Pink/future species without charred art **keep their original artwork**; no fake bare tree, replacement art or new charring effect is generated. Healthy seasonal/wet/snow foliage selection remains suppressed when burnt and returns on reset.
- SeasonalFoliageAdapter exposes PaletteCount, ChunkCount, MaskVariantCount, MaterialCount and PaletteUploads for local profiling. No diagnostic polling was added.
- SeasonalSaveCompatibility gates exact current Oak/environment/season resources, then reconstructs full historical dependency fingerprints using frozen pre-SE0 / SE0 / SE1 resource texts. It composes with existing independently gated terrain/layout bridges, so those changes do not cancel the seasonal bridge. Unrelated dependencies remain current and fully fingerprinted. Future resource edits disable the exact bridge until reviewed again.
- This phase does not change the current content fingerprint or snapshot schema. Opening an accepted older campaign does not rewrite files immediately; the existing next successful checkpoint records the current fingerprint. Manual season preview still resets on scene restart and is not saved. Blueprint/world IDs, removals and burn deltas retain their owners.

## Checks actually run

```sh
python TESTS/Seasons/check_foliage_shader_gl.py
python TESTS/Seasons/check_seasonal_compatibility.py
git diff --check
```

All passed. Mesa llvmpipe translated-fragment checks cover neutral/Summer, masks and fallback, bark/alpha, tone/strength/timing, rain/snow and actual fire erosion composition. Dependency checks reproduce the fingerprints of shipped commits 01237bec (pre-SE0), 45b55ff9 (SE0), 3156681a (SE1) across the known terrain bridge, verify the current fingerprint is unchanged, reject unrelated source mutations and verify old/current Oak placement text differs only by mask/palette wiring. They do **not** run C#, Godot's parser, campaign IO or the real renderer.

**Not run here:** dotnet build, Godot import/headless fixtures, rendered Windows checks, disk Continue migration or FPS/GC/cache measurements. Neither .NET nor Godot is installed. Do not mark SE2 accepted based solely on these checks.

## Local commands

.NET 8 / Godot Mono compatible with project Godot.NET.Sdk 4.7.2. Substitute your executable path:

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Seasons/ValidateSeasonalFoliage.tscn
godot --headless --path . TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --path . TESTS/Generation/ValidateRainSleeping.tscn
godot --headless --path . TESTS/Generation/ValidateSnowSleeping.tscn
godot --headless --path . TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
godot --headless --path . TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Expected seasonal fixture: **SE2 PASS**, plus one deliberate invalid-mask warning. It exercises shared wet/snow/fire state, fire cache sharing/release through a snow swap, no-charred-art completion/reset, eight bind/unbind cycles, current-season initialization, stable groups, neutral uploads and the native fingerprint acceptance/rejection API. P3 fixture now checks stable timing and current Autumn atlases after real traversal. Existing broader native shutdown/cancellation baseline failures are documented in root README; report exact commands/errors rather than labelling an incomplete suite passed. Save fixture uses its own validation storage, not real campaign folders.

For legacy **disk** validation, retain a disposable campaign produced by one of the listed historical commits and reopen it with this revision. Confirm same WorldId/map, player/clock/HP, supported removed/burnt trees, successful next save with the current fingerprint and restart. Do not edit or bypass a real campaign's stored fingerprint. Source reconstruction is not proof of disk restoration.

## Windows visual/profile acceptance

1. F6 World; without changing the day clock, select October and vary season strength/timing. Walk across chunks and back. Newly published foliage must have current colours immediately; each tree keeps its timing.
2. Toggle rain/snow, then change season while each is active. Orange/red leaves still darken and accumulate snow; bark remains excluded. Try mirrored trees and wind -3/0/+3, shadows/occlusion and no F9 panel.
3. Burn/reset Oak with snow on, off, and changed mid-burn. Charred art must not recolour. Burn a species without charred artwork: original image stays, completion stops and healthy leaf effects remain suppressed until reset. Future charred artwork can be added through the species/variant fire Resource.
4. Observe adapter diagnostics during traversal: palettes follow loaded species, materials follow chunk/source/mask/profile combinations, not timing buckets or individual trees. Detach releases counts; PaletteUploads stays unchanged when idle and while scrubbing a disabled/zero-strength preview.
5. Inspect active-fire cache after all fires/effect tails end: CachedShaderCount returns to its prior baseline. Measure idle, scrubbing, active weather/fire and repeated traversal with Godot profiler; no season per-tree callbacks or shader recompiles on slider movement. No measured FPS or memory improvement is claimed here.

Activation remains explicit season commands / chunk binding / ignition. State updates finish synchronously; only active existing weather/fire systems process. Cleanup is driven by chunk unload, fire completion/reset/cancel and scene exit. Seasonal draws retain SE1b's one state lookup, plus one tone lookup only when influence is active.
