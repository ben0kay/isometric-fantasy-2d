# Local light LL0–LL1 checks

Build C# and import assets, then run Godot .NET with `--headless --path . --script TESTS/Lighting/validate_local_lights.gd` for runtime checks. For image verification omit --headless and use Compatibility rendering. The test freezes the clock at night, isolates one existing torch and moves it to either side of a generated tree. It compares lit/shadowed ground pixel patches and saves user://local-light-shadow-±1.png / local-light-no-shadow-±1.png.

Checks all generated tree/rock blocker footprints with floating-point tolerance, mirror variants, idle occluders, three torches, shadow toggles, separate brightness/radius settings and retained sun casters. Test changes only runtime settings. Actual rendered results were verified in Godot 4.5.1 software OpenGL at full production scene density; target-machine rendering/performance still requires a local check.
