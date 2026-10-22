# Production fire integration check

After building C#, run Godot .NET with `--headless --path . --script TESTS/Fire/validate_production_fire.gd`. Omit --headless and use Compatibility rendering to also save burning/reset screenshots to user://. Uses fixed seed 213876 and temporary in-memory timing/config changes; production assets and placement counts are not altered.

Checks normal/mirrored oak alignment, idle receivers, repeated ignition rejection, collider stability, charred/reset shadows, wind reversal, snow toggles during burning, material restoration, leaf suppression/restoration, removable UI and effect caps. Requires an imported project and a .NET Godot executable.
