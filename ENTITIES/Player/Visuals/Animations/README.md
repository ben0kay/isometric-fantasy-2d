# Player / Visuals / Animations

Own the **Godot-side playback definitions** for player sprite animations rendered from Blender, such as `SpriteFrames` resources, directional animation mappings, and playback settings when needed.

- Animation state selection (idle, walk, run, etc.) will follow player movement/action state.
- Sprite image files belong in `../Sprites/`, not duplicated here.
- The Blender bone rig and source 3D actions belong in `../Source3D/`. A separate 2D `Skeleton2D` rig is **not necessary** for frame-rendered artwork.
- Direction count and animation state architecture remain open until tested.

**Status:** Empty documentation scaffold; no animations or scripts added.
