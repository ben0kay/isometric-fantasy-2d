# Player / Visuals

Visual presentation of the player: sprite display, facing direction, animation selection, sorting, and any player-specific attachments.

For this project, the **candidate pipeline is Blender 3D animation rendered into 2D sprites**, not a live 3D player and not a Godot Skeleton2D cutout rig.

- `Source3D/`: original Blender project/model/rig/texture sources (when available).
- `Sprites/`: exported 2D sprite sheets or frames with consistent camera angle, scale, and feet origin.
- `Animations/`: Godot playback resources and directional animation mapping when implemented.

Keep rendering separate from movement and inputs so character art can be replaced without changing gameplay. Evaluate pixel-art filtering, sprite size, and directional coverage in a sandbox.

**Status:** Documentation/folder only; no artwork or animation code yet.
