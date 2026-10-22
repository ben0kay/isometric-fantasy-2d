# Shared scenery definitions
SceneryDefinition owns a stable ID, display name, reusable scene variants, conservative spacing/artwork extents and scene-local sprite/footprint paths. All variants of a definition must satisfy these paths and extents. Use a separate definition when footprints/extents differ substantially. Variants are selected with equal probability using the world RNG.

Scene roots must be Node2D (StaticBody2D for collidable props); artwork and collisions use a foot/root anchor. Existing oak/rock collisions are copied from proven debug scenes. Placement mirrors the polygon and Sprite2D together without negative body scaling. Spacing is conservative circular clearance, not an arbitrary geometry-overlap solver.
