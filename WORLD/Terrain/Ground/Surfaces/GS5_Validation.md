# GS5 — optional Oak dirt/rock and all-layer F9 sliders

Oak now references two reusable production profiles: Dirt.tres / Textures/Dirt.svg and Rock.tres / Textures/Rock.svg. These are restrained 32px hand-authored procedural SVG textures with Granular/ Mineral detail styles, not promoted DEBUG decals. Extra A is broad earth (400-unit mask scale); Extra B is smaller/sparser rock (150). Each has independent salt, warped edges, coverage and opacity. Existing grass base, accompanying colours/noise and Pink Meadow are unchanged.

Both extra Resources have Enabled=false and AllowSessionPreview=true. They are **off by default**, including after reset/restart. Streamed setup preloads only their small shared atlas/table data so F9 can show them immediately without modifying Resources or rebuilding maps. A normal disabled layer without AllowSessionPreview remains unavailable. Current all-off visibility is a uniform fast path that skips optional-layer sampling entirely.

> Appearance revision: the SVG textures described in the original GS5 handoff were rejected and replaced with old-project Dust/Basalt shader recipes. See PROCEDURAL_GROUND_RECIPES.md for current profiles, masks, removed assets and evidence. F9/default-off behavior below remains applicable.

## Try in regular Oak World

Open SCENES/World/World.tscn, F6 → F9 → Ground Surfaces. In Oak:

- Base fine detail: toggle + strength slider (base ground remains full coverage).
- Accompanying grass patches: toggle + strength slider.
- Show extra patch layers: master toggle.
- A / Dirt patches: independent visibility, coverage and opacity sliders.
- B / Rocky patches: independent visibility, coverage and opacity sliders.
- Reset Ground Preview: restores grass/companion and turns dirt/rock back OFF.

Slider multipliers: 0 removes that contribution, 1 is authored strength, up to 2 increases it (coverage/opacity/patch strength clamp appropriately). Colour/tint remains authored in the biome Resource. Settings are temporary global comparisons, not per-biome saved edits. Missing Pink slots stay absent even when forcing A/B on; shared base/companion strength sliders affect both biome families. Close F9 without losing the preview; restarting restores defaults. The old compatibility preview remains base/companion only.

No ground tile nodes, per-chunk materials, frame callbacks or per-slider texture rebuilding were added. Hidden panels still sleep; uniform writes only occur on changed values. Zero base-detail/companion strength also skips corresponding noise work. The 32px atlas has room for both new tiles in its previously unused cells with the current two-biome configuration; GPU cost while previews are ON still requires target profiling.

## Evidence and limitations

Software Mesa/llvmpipe checks passed: original default fragment output max error 0.0, negative-coordinate sixteen-chunk parity, A/B/multipliers/reset, zero base-detail and accompanying strength versus their toggles, and default-off configured-layer activation producing the same result as authored-enabled layers. These tests translate the shader calculation subset to GLSL; they do not validate Godot C# or its shader importer.

Static checks passed for dependency references, unchanged Pink/base profiles, exact migration hashes and frozen pre-GS5 Oak text. Engine fixtures were updated for authored-off preview data, independent activation without table rebuilding, reset back to off, new sliders and previous-save compatibility. Godot/.NET are unavailable in this runner, so engine fixtures, Windows visuals and target performance remain unverified for GS5. Previous user checks predate this change.

The strict save bridge substitutes frozen pre-GS5 Oak resource text only when exact current Oak/Pink/Dirt/Rock resource hashes match. Added-profile dependencies are excluded only during that explicit historical reconstruction; unrelated changes still reject. Earlier known ground migrations remain narrowly supported. Supported world identity/edits/collision are unchanged. Verify real Continue on the PC.

Run the local build/import and Terrain/DebugUI/Generation/Save checks in GS4_Validation.md. Compare Base only / Dirt / Rock / Both at fixed camera/time/weather, check Pink border and chunk unload/reload, reset/restart and record actual GPU/frame pacing before approving permanent authored-on layers. GS5 is implemented, awaiting visual approval; GS6 profiling/general QA remains pending.
