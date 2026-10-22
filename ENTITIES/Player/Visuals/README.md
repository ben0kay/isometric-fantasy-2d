# Player / Visuals

Future **2D top-down** player presentation: directional sprites, feet/origin alignment, facing, animation playback, equipment overlays and status appearance (injury/wetness). Keep visuals replaceable without changing movement or game-state logic.

An optional future workflow is to create/rig/animate a source model in Blender, then render it into 2D spritesheets. Blender source materials can eventually live in `Source3D/`; the Godot runtime itself uses 2D sprites, **not Sprite3D**, and production art may live in `Sprites/` and `Animations/` when created.

**Current state:** This is documentation/folder scaffolding only. The temporary explorer artwork and sprite code are entirely in `DEBUG/TopDownTest/`. Do not confuse those debug assets with the eventual production player rig.
