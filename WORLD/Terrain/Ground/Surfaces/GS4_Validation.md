# GS4 ground controls — implementation and combined testing handoff

Implemented as requested after GS2/GS3. The author subsequently reported regular World/practice checks passed. Those checks predate the later accompanying-layer extraction; see ACCOMPANYING_PATCH_LAYER.md for its pending validation. GS5 production Oak dirt/rock authoring is not implemented.

## Behavior and ownership

F9 → Ground Surfaces is the third exclusive drawer inside the existing EnvironmentDebugPanel. Environment/Combat categories, Environment-only diagnostics, fire hosting and F7 retain their owners. Panel shows current biome and base profile, accompanying patches/base detail, overlay master, independent A/B toggles, coverage and opacity multipliers, and Reset Ground Preview.

Production GroundSurfacePreview belongs to the world/streamed material (or legacy private preview material). It has no Node or callbacks and no DEBUG dependency. Input writes only changed shader uniforms; multiplier values are bounded and finite. Materials and uniform state are unique per world. No Resources are mutated, ResourceSaver is never called and no campaign fields are added. Hiding F9 retains overrides; reset returns authored base flags and neutral multipliers, and loading a new scene starts fresh. Old preview has base controls only.

Overlay multipliers are **global**, not per-biome authoring controls. They affect compiled, authored-active overlays; disabled, zero-authored-strength and missing layers remain absent. Coverage is mask threshold bias, not exact area percentage. 1 is authored strength, 0 hides and up to 2 increases effective coverage/opacity (clamped in shader). Opacity/coverage/visibility do not rebuild maps. Base colour variation and fine detail toggles affect only base appearance; artwork and base coverage remain.

The Ground UI polls only while visible (4Hz for labels/widget sync), sleeps when hidden/collapsed, and never drives production rendering. Its current-biome lookup uses existing macro geography and cached definitions. No world/group scans or hidden status timer. Controls do not take movement-key focus. Slider events update the shader immediately; labels refresh at 4Hz. Production renderer has no added frame callback. GPU branches skip disabled variation, fine detail and optional layers; target GPU cost remains unmeasured.

## Checks actually run

`python TESTS/Terrain/check_ground_shader_gl.py` on Linux Mesa 25.2.8 / llvmpipe OpenGL 4.5 passed:

- Pre-GS2 default fragment output still matches exactly (maximum absolute difference **0.0** with synthetic shared inputs).
- Deterministic two-layer composition and sixteen separately rendered 512-unit chunks at negative coordinates remain correct.
- New master preview switch restores base; preview A/B switches match authored A/B disable results.
- Zero coverage/opacity multipliers restore base; accompanying patches/base detail toggles affect expected pixels; neutral reset restores the original rendering.

Static whitespace checks, source inspection and unchanged production resource/scene text checks passed. Engine fixtures are extended for third-category exclusivity, hidden polling, remembered selection, widgets/reset, no authored-content mutation and cross-world preview isolation.

**Not run:** C# compilation, Godot shader importer, engine/UI/headless fixtures, Windows graphical gameplay, real campaign restore and profiling. This runner has neither Godot nor dotnet. Software GL does not prove those checks pass. No target FPS claims.

## Test everything when back at the PC

1. Pull this commit; open in project-compatible Godot .NET 4.7.2 / .NET 8. Build and inspect Output for C# or shader errors.
2. Open SCENES/World/World.tscn, F6. Default Oak/Pink should look unchanged. Walk, cross chunks/biomes, check shadows/weather/snow and normal combat. Existing production overlays are empty, so Ground A/B entries should say **not configured here**, not display unexpected dirt.
3. F9 → Ground Surfaces. Compare accompanying patches/base detail; verify Environment and Combat still work and only Environment shows right diagnostics. Hide/show F9 and collapse/reopen Ground: selected state and overrides must persist. Reset should restore authored look. Check movement keys and a smaller window.
4. Open TESTS/Terrain/GroundLayersPractice.tscn, F6. Sand left and Soil right; F9 opens **the same Ground drawer** with two configured layers. Toggle A/B separately and master off; drag coverage/opacity sliders, reset, check biome edge/chunk seams/negative coordinates, Z zoom. 1/2 are shortcut toggles, R resets. Fixture's status/control labels describe the Sand side. It changes no production files.
5. Restart either scene: no comparison overrides should leak. Run a campaign/Continue and SAVE's existing restore handoff; ground previews must not affect checkpoint compatibility/content/edits. The original GS1 schema compatibility conditions remain unchanged.
6. Capture fixed-seed/camera/time/weather Oak/Pink/border comparisons and target profiling as listed in GS0_GS1_Validation.md and GS2_GS3_Validation.md. Idle/active/shutdown/reactivation and walking/streaming CPU/GPU/memory checks remain required.

Optional automation, from repository root (`godot` may need its full executable path):

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Terrain/ValidateGroundStack.tscn
godot --headless --path . TESTS/Terrain/ValidateGroundLayers.tscn
godot --headless --path . TESTS/DebugUI/ValidateDebugHud.tscn
godot --headless --path . TESTS/Generation/ValidateMacroGeneration.tscn
godot --headless --path . TESTS/Generation/ValidateChunkStreaming.tscn
```

Require zero exits, explicit PASS and no shader errors; report actual failures and native shutdown errors separately. TESTS/Save/README.md and SAVE/P4_Copilot_Handoff.md own disk regression commands. Tests should first run without editing code. All GS0–GS4 graphical/performance acceptance remains pending until these local results are available.
