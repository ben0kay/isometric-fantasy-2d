# Player / Visuals

Future **2D top-down** player presentation: directional sprites, feet/origin alignment, facing, animation playback, equipment overlays and status appearance (injury/wetness). Keep visuals replaceable without changing movement or game-state logic.

An optional future workflow is to create/rig/animate a source model in Blender, then render it into 2D spritesheets. Blender source materials can eventually live in `Source3D/`; the Godot runtime itself uses 2D sprites, **not Sprite3D**, and production art may live in `Sprites/` and `Animations/` when created.

**Current production visual:** `Sprites/Player01.png` is the static character texture in `../Player.tscn`, scaled to 0.17 and offset by half the source image height so the feet remain at the player's ground/Y-sort origin. The existing shadow, camera, physics collider, health-bar anchor, snow/wading visuals and equipped-tool attachment remain separate; changing appearance should not move the CharacterBody2D. The former `Sprites/player.svg` is no longer used by production. The independent `DEBUG/TopDownTest/` artwork is unchanged.

**Still planned:** Directional movement/attack animation, equipment overlays, rig/sprite-sheet import and appearance variants. The new image is one static portrait, not an animated character.
