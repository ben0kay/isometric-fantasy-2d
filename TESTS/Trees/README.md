# Generic tree integration checks

Build C#, then run Godot .NET with `--headless --path . --script TESTS/Trees/validate_generic_trees.gd`. For software/desktop visual validation, omit --headless and use Compatibility rendering. A screenshot is saved to user://generic-tree-blossom-burning.png. Tests alter only runtime Resources and use the full production population density.

Checks the single-template contract, normal/mirrored ground anchors and polygons, idle processors, tool-query hits and sprite-only wobble, species leaf/petal config, both-species snow/wind, generic fire including a missing charred image, reset, fixed-seed replay, and a third species with two variants created entirely from Resource data. The fixture tests optional disabled hit/fire, no wind material, centered/offset asymmetric artwork and deterministic variant selection. TESTS/Fire preserves the charred oak and shadow regression checks; DEBUG startup is checked separately.
