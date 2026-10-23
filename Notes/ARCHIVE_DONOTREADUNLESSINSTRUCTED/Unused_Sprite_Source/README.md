# Unused source sprite archive

Original root-level artwork retained for possible future reference. **Not active Godot game content**; no production scene, Resource or C# loader references these files. The empty `.gdignore` in this folder excludes the archive from Godot's FileSystem, import process and exports.

- `Charred Oak Tree Sprite.png`: alternative/unused charred-oak artwork. Production uses `WORLD/Content/Vegetation/Trees/Oak/Sprites/oak_charred.png`.
- `Fantasy Pink Blossom Tree Sprite.png`: old unreferenced image. Production uses `WORLD/Content/Vegetation/Trees/PinkBlossom/Sprites/tree_pink_blossom_01.png`.
- `Pixel-Art Mossy Boulder Sprite.png`: old unreferenced image. Production uses `WORLD/Content/Rocks/Sprites/rock_01.webp`.

These source files were moved, not recompressed or edited. Their old root-level Godot `.import` sidecars were removed, because archived images must not be imported. If a variant is approved for future gameplay, copy it into the relevant canonical content folder and define/validate its Resource/scene references there. Do not load sprites from this archive.
