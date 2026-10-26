# DEBUG — Independent experiments and removable panels

Whole-world metadata preview: open `PreviewMap/PreviewMap.tscn` and press F6 for seed/regenerate and Biomes/Climate/Water, without any gameplay chunks. See [PreviewMap guide](PreviewMap/README.md).


[TopDownTest/TopDownVisualTest.tscn](TopDownTest/TopDownVisualTest.tscn) is the preserved 2D forest sandbox. Build Godot .NET and run this scene directly with **F6**. Its current map is **3840×1728**, with Oak Forest/Pink Meadow; see [TopDownTest README](TopDownTest/README.md).

**F5 now starts Boot**, via `project.godot`, not a debug experiment. Production test: open [World.tscn](../SCENES/World/World.tscn) with F6 or use the P4 campaign menu through F5.

[DEBUG/UI](UI/README.md) owns removable production F7/F9/fire controls. Removing a panel does not remove the production system behind it.

`VisualTests/` contains retired 3D files, including `NaturalTerrainTest.tscn`, not an F5 default. Its obsolete instructions are preserved in the [Notes archive](../Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/NATURAL_TERRAIN.md). Courtyard scenes and a VisualTests README are no longer present.
