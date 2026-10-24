# SE3 — mask contract and authoring regression coverage

**Implemented; awaiting native Godot/Windows validation.** Wind behaviour, production artwork/resources, seasonal palettes, weather/fire appearance and save schema are unchanged.

## Added

- FoliageMaskValidation: small metadata-only full-canvas resolver. Null mask silently falls back; source/mask dimensions and atlas-region incompatibility produce actionable diagnostics. SeasonalFoliageAdapter caches resolution per loaded variant, exposes MaskValidationChecks and releases entries on unload. No runtime image readbacks or new process callbacks.
- Offline PNG/import audit with self-tests and explicit pair mode, plus discovery of canonical inline/standalone .tres variant ExtResource pairs. Checks grayscale/red-channel contract, opaque alpha, black transparent padding, dimensions and import settings; never modifies files.
- Actual Oak software-GL alignment fixture exercising 18 cases: three source-to-world scales, both mirrors and wind -3/0/+3. It renders the production wind fragment/foliage selection against the real mask and compares CPU reference samples, including grey edges, alpha and wind margins. It verifies that using unwarped UVs would fail the fixture.
- Native ValidateFoliageMasks fixture for real Oak, missing/invalid/atlas masks, cached validation, mirrored/scaled/centered/offset sprite variants, rooted-anchor preservation and unload cleanup. Native fixture has **not** been compiled/run here.
- [Authoring guide](../../WORLD/Content/Vegetation/Shared/Seasonal/MASK_AUTHORING.md) covers Avalonia exports, assignment/import, gradual adoption and the actual row-weighted wind behaviour.

## Checks run

```sh
python TESTS/Seasons/check_foliage_masks.py --self-test
python TESTS/Seasons/check_foliage_alignment_gl.py
git diff --check
```

Passed. Current Oak source audit: **222×295**, opaque grayscale mask, **19,758 visible selected pixels**, including **3,831 grey-weight pixels**, **zero selected transparent-padding pixels**. Existing neutral/mask/fallback/season/weather/fire software-GL assertions also pass through the reused harness. This is selection-format/alignment evidence, not proof that every intended leaf was artistically selected.

The older `check_seasonal_compatibility.py` source test currently fails its first historical fingerprint comparison on baseline 6014c58 (before this task): expected 7ACA6A38... versus ECAF03CF.... SE3 does not modify that test or any production .tres/.tscn dependency. Later biome/resource changes invalidate its frozen full-graph expectation; broader save compatibility needs a separate review. Do not describe that check as passed or bypass content validation to silence it.

**Not run:** .NET build, Godot import, native tests, Windows rendering or profiler measurements. Neither dotnet nor Godot is installed here. GL translation does not test Godot's parser, vertex geometry, native API or draw batching.

## Local commands / acceptance

Use the project's .NET 8 / Godot Mono tooling:

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Seasons/ValidateFoliageMasks.tscn
godot --headless --path . TESTS/Seasons/ValidateSeasonalFoliage.tscn
```

Expect **SE3 PASS** from the new fixture. Existing SE2 fixture still includes exact save-bridge assertions; unrelated authored resource changes can separately fail those assertions. Preserve the explicit production FireErosionAdapter alias added in 6014c58 (a global DEBUG class has the same name).

For rendered acceptance use F6 World. Inspect dark outer leaves, bark, leaf gaps and grey mask edges under Autumn, rain and snow; repeat with wind -3/0/+3 and mirrored/scaled artwork. Source and selection must move together with no halo/offset. Native fixture covers offset/centred rooted alignment; direct software-GL cases do not render Godot scene transforms. Missing/invalid masks must fall back without changing the source sprite or anchor. Check MaskValidationChecks remains stable while scrubbing and releases on chunk unload. Mask quality refinements remain optional external Avalonia work.

Activation is initial chunk binding only. Cache hits do not revalidate; season events do not call validation. Offline image scans run only when explicitly invoking the Python tool. SE4 automatic calendar/gameplay seasons remain deferred.
