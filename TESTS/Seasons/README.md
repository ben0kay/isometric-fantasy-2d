# Seasonal foliage checks

`python TESTS/Seasons/check_foliage_shader_gl.py` translates the production fragment/functions to software GL. Requires Linux Mesa EGL/GL and numpy; checks exact neutral paths, mask/fallback selection, grey weights, luminance/alpha, autumn rain and snow. This does not run Godot or prove renderer/instance-uniform support.

With Godot Mono imported/built: `godot --headless --path . TESTS/Seasons/ValidateSeasonalFoliage.tscn`. The isolated production-tree fixture checks material sharing, palette texture propagation through snow copies, missing/invalid-mask fallback, burnt/reset gating and reference cleanup. The invalid-size mask deliberately emits one warning. This fixture is authored but not run in the implementation environment.

See ENVIRONMENT/Seasons/SE1_Validation.md for visual/P3/P4 checks and limitations.

SE1b extends software-GL checks with the real authored Oak palette, January wrap, Summer plateau, pale winter lift, strength interpolation and timing-atlas selection. Native fixture adds date mapping, strength, atlas size, stable ID buckets and early/late variation. Native fixture remains unrun here.

SE2: run `python TESTS/Seasons/check_seasonal_compatibility.py` for historical source fingerprint reconstruction and rejection checks (requires repository git history). Shader checks now compile/render the actual erosion function with seasonal/weather composition. Native fixture now expects **SE2 PASS**, adds no-charred-art cleanup, rain/snow/fire material propagation, reference-counted fire cache teardown, eight rebind cycles and compatibility API checks. Native/P3/disk tests remain unrun here; see ENVIRONMENT/Seasons/SE2_Validation.md.
