# SCENES

`IsometricVisualTest.tscn` is the main scene: a 24m enclosed courtyard with a terrace, ~14° ramp, arch, pillars, crates, blue shrine landmark, gold spawn tile, sun and ambient environment. Scenery lives under Ground, Bounds, Ruins, Props and Landmark. Meshes and accurate box colliders are authored in the scene; no runtime world generator.

Open project.godot in **Godot .NET** matching the project's 4.7 version, build C#, then F6 for this scene or F5 for the project. The csproj supplies the missing C# project scaffold (Godot.NET.Sdk 4.7.0 / net8.0); restore/build must be verified locally against your installed editor/.NET SDK.

Arrow keys move relative to the camera. Walk up the central ramp to the northern arch, around crates and into perimeter walls. Check diagonal/cardinal speed, slope grounding, stationary camera orientation, sunlight/shadows and sprite import. Walk behind a pillar to inspect real depth occlusion. Releasing arrows should stop horizontal movement.

Camera and Player master configs own feature settings; the existing blank ScenesMasterConfig remains reserved because this authored test has no runtime scene settings. Geometry/materials and lighting are editable directly in the scene Inspector.

**Validation:** source/reference checks only. No C# build, Godot load, screenshot or movement test could be run in the implementation session.
