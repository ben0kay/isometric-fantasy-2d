# Seasonal foliage checks

`python TESTS/Seasons/check_foliage_shader_gl.py` translates the production fragment/functions to software GL. Requires Linux Mesa EGL/GL and numpy; checks exact neutral paths, mask/fallback selection, grey weights, luminance/alpha, autumn rain and snow. This does not run Godot or prove renderer/instance-uniform support.

With Godot Mono imported/built: `godot --headless --path . TESTS/Seasons/ValidateSeasonalFoliage.tscn`. The isolated production-tree fixture checks material sharing, palette texture propagation through snow copies, missing/invalid-mask fallback, burnt/reset gating and reference cleanup. The invalid-size mask deliberately emits one warning. This fixture is authored but not run in the implementation environment.

See ENVIRONMENT/Seasons/SE1_Validation.md for visual/P3/P4 checks and limitations.
