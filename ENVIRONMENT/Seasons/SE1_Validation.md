# SE1 — masked Oak and seasonal palettes

Implemented: optional TreeVisualVariant.FoliageMask, optional TreeDefinition.SeasonalFoliage, Oak mask/palette references, shared selection/recolour include in VegetationSway, world/chunk adapter and event-driven per-profile state texture. F9 controls now recolour Oak. Pink Blossom has no seasonal profile. Weather selection uses original source RGB or mask; existing burn completion/reset only sets a healthy-selection gate. No new fire effects, calendar or save schema.

## Checks actually run

- `git diff --check`: passed.
- `python TESTS/Seasons/check_foliage_shader_gl.py`: passed on Mesa llvmpipe 4.5. Compiled translated production fragment plus seasonal/wet/snow functions and rendered synthetic pixel cases. Verified disabled/Summer identity, black-mask bark protection, fractional mask influence, source alpha, masked dark leaves, unmasked green fallback, luminance preservation, autumn rain darkening, snow on masked dark leaves and burnt selection suppression.
- Static review: attachment order, same warped UV, shared texture identity across material duplication, chunk refcounts, missing/invalid mask fallback, no palette/controller frame callbacks, and no resource mutation on slider edits.
- **Not run:** .NET build, Godot import/headless fixture, native renderer/instance-uniform validation, Windows visual checks or FPS profiling. Neither dotnet nor Godot is installed here. Software GL translation is not a Godot parser/API test.

## Local commands

Use Godot Mono 4.7.2-compatible tooling / .NET 8; substitute your executable path for `godot`.

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Seasons/ValidateSeasonalFoliage.tscn
```

Expected fixture: SE1 PASS, including one deliberate invalid-mask warning. Then run existing rain/snow/generic-tree regressions:

```sh
godot --headless --path . TESTS/Generation/ValidateRainSleeping.tscn
godot --headless --path . TESTS/Generation/ValidateSnowSleeping.tscn
godot --headless --path . --script TESTS/Trees/validate_generic_trees.gd
```

## Visual acceptance

1. F6 SCENES/World/World.tscn. F9 → Environment → Seasonal foliage → Enable. Summer 0.25 must match original Oak; toggle must restore the original immediately. Pink Blossom remains pink.
2. Scrub Spring 0 / yellow ~0.40 / orange 0.50 / red ~0.60 / brown Winter 0.75 / Spring 1. Inspect dark outer leaves, bark gaps and roots. Grey mask regions intentionally blend partially; black regions remain unchanged. If actual leaves remain unselected, refine the mask in Avalonia.
3. Try wind -3/0/+3, mirrored/scaled trees, occlusion and shadows. Mask boundaries must move with the artwork. Compare source and mask in the editor if any fringe appears.
4. Rain darkens autumn leaves; snow covers selected dark leaves without recolouring bark. Turn snow off/on while scrubbing; the live season must persist. Snow deposits remain noise/brightness driven, not uniformly white.
5. Ignite/finish/reset existing fire: charred art must not gain seasonal colours, reset must restore selected season. No new burn appearance is introduced.
6. Walk across streamed chunk boundaries at Autumn and back; no green startup flash. Inspect SeasonalFoliage adapter PaletteCount/ChunkCount during loading/unloading and confirm cleanup. Profile idle, disabled, scrubbing, rain/snow and movement; no adapter/controller processing callbacks or per-tree season loops. No measured performance claim yet.

## Campaign limitation (SE2 follow-up)

**Use F6 or a new campaign for SE1.** The strict CampaignSession content fingerprint includes Oak.tres and referenced .tres Resources. Oak's new mask/profile references therefore change the fingerprint; older campaigns may reject Continue. No save data is rewritten, schema changed or compatibility check bypassed. A narrowly validated appearance-only migration is deferred to SE2; do not describe old-campaign compatibility as accepted yet. Preview season state remains session-only.

## Performance contract

Explicit season commands wake only currently referenced palette updates, then finish synchronously. Material creation/variant validation happen on chunk binding only. Material groups scale with loaded chunk/source/mask/profile; one state texel per species is shared even by snow/fire copies. Chunk detach releases registrations and refcounts. There is no season idle CPU loop; rendered opted-in foliage performs one state texture fetch to determine zero/active influence. Target hardware FPS, memory and cache stability remain unmeasured.
