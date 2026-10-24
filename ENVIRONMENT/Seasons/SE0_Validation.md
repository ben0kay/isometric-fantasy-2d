# SE0 implementation and validation

Implemented: SeasonMasterConfig, default Resource, SeasonController, EnvironmentRuntime composition, optional EnvironmentMasterConfig reference and existing F9 Environment section. Plan revised to use the existing Oak mask in SE1 while preserving unmasked weather detection. No tree/material/save changes.

## Checks performed

- `git diff --check`: passed.
- Static review: disabled/Summer defaults, absent-config fallback, private scalar runtime state, finite/clamped inputs, nearest-quarter labels including 0/1 Spring, unchanged-value event suppression, immediate slider label update, and no controller frame callbacks.
- Build/import/runtime/profiling **not run**: neither `dotnet` nor `godot` is installed in this environment. Implementation awaits local validation; static review is not a build or FPS result.

## Local acceptance

From repo root with .NET and Godot Mono installed (`godot` below means your Mono executable):

```sh
dotnet build
godot --headless --editor --path . --import
```

1. Open SCENES/World/World.tscn, F6, F9 → Environment. Seasonal foliage follows Time and starts collapsed.
2. Expand: disabled, year 0.25, Summer. Scrub 0/0.25/0.5/0.75/1: Spring/Summer/Autumn/Winter/Spring. Both endpoints stay selectable. Leaves stay unchanged in SE0.
3. Pause clock; scrubbing and Enable still work. Toggle off/on; selected year remains. Existing wind/rain/snow/combat/ground controls still work.
4. Restart disposable World: defaults return. Temporarily remove debug UI and run: production Seasons still exists. Temporarily clear Seasons config: default state remains safe. Restore authored scene/config after checks.
5. Inspect profiler: SeasonController adds no process/physics callback. Scrubbing emits changes only; no chunk regeneration, tree loops, material creation or save writes. Walk across chunks and confirm normal streaming.
6. Existing campaign Continue should retain its existing behavior; no save schema or content definitions changed.

Activation: explicit Initialize / SetEnabled / SetYearPosition. Shutdown: each command finishes synchronously; nothing remains scheduled. State lives with EnvironmentRuntime and is freed with the scene. No new season per-frame CPU work; existing visible F9 refresh is reused. FPS has not been measured.
