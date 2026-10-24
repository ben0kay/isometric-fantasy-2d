# SE1b — understandable year preview and organic foliage

User reported SE1 working. This approved follow-up adds January-based month/date labels, pale winter tone, strength and stable timing variation. SE2 broader performance/save integration and SE4 automatic calendar remain open.

## Changes

- SeasonController: January 1 is normalized 0/1, early July is 0.5; default disabled/Summer. A non-leap 365-day date label uses Northern Hemisphere seasons. State is still manual/session-only, independent of GameClock.
- F9 existing Seasonal foliage section: date/day slider, colour strength 0–100%, early/late variation ±0–45 days (default 14).
- Species Palette Gradient retains RGB/influence; optional ToneControls Gradient encodes saturation in R and brightness lift in G. Oak winter is pale blue-green, with shadow-preserving lift; summer has a wide exact-source plateau. Strength scales colour/tone together. Mask/fallback/weather contracts remain intact.
- One shared 16×2 float texture per referenced species profile. Row 0 colour/influence; row 1 tone. 16 deterministic timing groups shift each tree's local preview date. Seasonal bucket comes from a stable world instance ID, or fixed content/rooted position for compatibility previews. It is set once on the sprite as an instance uniform; no new materials for variation.

## Checks run

`git diff --check` passed. `python TESTS/Seasons/check_foliage_shader_gl.py` passed under Mesa llvmpipe. Existing neutral/mask/fallback/weather tests pass, plus real authored palette tests for January/December wrap, Summer plateau, pale winter luminance, zero/half/full strength, synchronized timing at zero variation and distinct early/late sampled colours.

.NET/Godot remain unavailable here. Native API compilation, import, the updated Godot fixture, Windows visuals and profiling are **not run**. Software-GL checks do not validate Godot instance uniforms or actual draw batching.

## Local checks

Use .NET 8 / Godot Mono compatible with this project's Godot.NET.Sdk 4.7.2. Substitute your Mono executable for godot:

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Seasons/ValidateSeasonalFoliage.tscn
```

The fixture deliberately emits one invalid-mask warning; otherwise expect SE1 PASS. Use **F6 World or a new campaign** for rendering; resource fingerprints changed again, and no older-campaign migration is included.

1. F9 → Environment → Seasonal foliage: enable; default date is July/Summer. Month/date label tracks the day slider; first/last both show January 1/Winter. Paused GameClock must not block scrubbing.
2. Try January, April, July, September, October, November, December. Winter should be pale muted cool foliage, with detail retained; enable snow separately to see brighter snow deposits.
3. Strength 0 restores original foliage; 50 is subtler; 100 is full. Rain/snow remain independent. Bark, roots and source alpha remain protected.
4. October with ±14 days should mix yellow/orange/red across Oaks. Variation 0 synchronizes their seasonal timing; 30 exaggerates it. Summer remains original at default variation; very large offsets can intentionally reach transition dates.
5. Walk away across chunks and return: individual trees retain their timing. Scrub while snow/fire materials are active; shared state propagates. Burnt/reset selection protection still works. Pink stays unchanged unless separately opted in.
6. Profile idle, scrubbing and traversal. No new per-tree/adapter/controller process callbacks; material count must not increase by 16 groups. Palette count follows referenced species; chunk unload clears registrations. Visual FPS/batching/memory are not measured here.

Activation: explicit control commands fill at most 32 data texels per live species, then finish. Only changed atlases upload. Group assignment occurs at binding. No global tree scan, runtime image readback, shader rebuild or per-tree loop on a slider change. Active fragments fetch an extra tone texel; zero influence exits before that fetch. Season preview is not saved.
