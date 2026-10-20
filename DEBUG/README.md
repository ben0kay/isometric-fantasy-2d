# DEBUG

Disposable development content lives here. `VisualTests/` owns both comparison scenes, their temporary Player scene and **all prototype SVG artwork**. Camera/player C# components and master configs remain in their feature folders.

- `VisualTests/Courtyard3DProps.tscn`: original courtyard, 3D mesh props and sprite player.
- `VisualTests/Courtyard2DProps.tscn`: same 3D floor, ramp, terrace, camera and player; all visible props are 2D SVG cards. This is now the project main scene.
- `VisualTests/Player.tscn`: disposable assembly referencing reusable player scripts.
- `VisualTests/Artwork/`: original SVG explorer, crate, pillar, arch, wall, shrine and tree sources.
- `DebugMasterConfig.cs` / `DefaultDebugConfig.tres`: existing reserved config; no new runtime debug settings are needed for these authored scenes.

Build C# in Godot .NET, then F5 for 2D props. Open either scene and press F6 to compare. See VisualTests/README.md for limitations and removal instructions.
