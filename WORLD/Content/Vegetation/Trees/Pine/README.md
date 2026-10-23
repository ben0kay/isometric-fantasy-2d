# Pine — artwork staged, not implemented

Two source sprites (`Sprites/Tree_Pine01.png` and `Sprites/Tree_Pine02.png`) were moved out of the project root during the sprite audit. Their Godot `.import` files retain the original texture UIDs and have updated source/import paths.

**Art only:** there is no Pine `TreeDefinition` Resource, variant configuration, authored trunk collision/anchor, biome population, or Pine runtime spawn yet. The game does **not** currently generate or display these trees.

To enable Pine later, follow [the shared tree authoring contract](../README.md): create a canonical `Pine.tres` Resource and visual variants with root anchors, collider polygons, visual bounds, compatible snow/wind/fire options and a biome population reference. Reuse `Shared/Tree.tscn` without a new Pine-specific controller.
