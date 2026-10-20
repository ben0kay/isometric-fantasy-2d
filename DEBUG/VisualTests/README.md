# Disposable visual comparisons

| Scene | Terrain | Visible props | Player |
| --- | --- | --- | --- |
| Courtyard3DProps.tscn | 3D meshes/colliders | 3D meshes | 2D PNG Sprite3D |
| Courtyard2DProps.tscn | Same 3D terrain | Flat sprite cards (tree PNG; other SVG) | Same 2D PNG player |

Open either scene in Godot .NET and press F6 after building C#. F5 starts the 2D-prop comparison. Arrow keys move relative to the same fixed camera in both. Spawn, terrain, ramp, lighting, camera settings and movement match; the prop layout is comparable rather than an exact object-for-object conversion. The sprite scene adds three trees.

Disposable prop art belongs to Artwork/. The trees now use `Artwork/tree_01.png` (~4.8m upright card) and the player uses the reusable `ENTITIES/Player/Visuals/Sprites/player.png` (~1.8m card). Remaining temporary props are SVGs rasterized by Godot. Sprite3D uses nearest filtering and alpha scissor. Sprites are upright, shaded, double-sided and cast double-sided cutout shadows. Their yaw is authored at 45° to match the default locked camera, with no per-frame billboard logic. If experimenting with camera yaw, rotate these cards to match. Plane lighting and shadows will differ from volumetric meshes; this is part of the comparison. No hand-painted projection or directional sprite sets are being proven here.

Only terrain (floor, path, spawn tile, terrace, ramp) has visible meshes in the 2D-prop scene. Walls around the floor retain invisible safety colliders. Visible props have invisible, approximate 3D collision footprints; the arch has separate legs and lintel, leaving a passage. Foliage is passable around its narrow trunk. Full alpha silhouettes do not automatically become physics collision. Foreground props can hide the player; no transparency/cutaway system exists.

The test Player.tscn and temporary prop art are here; the player PNG is now a reusable asset under ENTITIES/Player/Visuals/Sprites/. Reusable ENTITIES and CAMERA scripts do not reference DEBUG art. Both scenes refer outward to reusable scripts/configs.

## Removal

Replace or clear Project Settings → Application → Run → Main Scene before deleting DEBUG/VisualTests/ (or the entire DEBUG folder). There are no other runtime references into this test folder. Keep reusable camera/player scripts and configs, or remove them separately if no longer useful. Notes/README links describe historical work and can be updated when deleting tests.

## Validation

Static checks: scene resource paths, load_steps counts, subresource IDs, scene-tree parents, removed-path references and sprite-only prop rendering. Godot import/rendering, C# build, movement/collisions and sprite shadows still require a local play-test; no runtime test is claimed.
