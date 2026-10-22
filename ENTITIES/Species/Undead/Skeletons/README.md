# Skeletons

- `Basic/SkeletonBasic.tres` and `Basic/Sprites/skeleton_basic.svg`: the resource-driven Basic variant.
- `Shared/`: future skeleton-family-specific assets.
- `ENTITIES/Shared/Enemies/Enemy.tscn`: one common enemy scene/controller for all supported enemy Resources, including other species.

World's temporary `EntityPlacement` points to the shared scene with the Basic Skeleton definition. Damage from the player's axe reaches the shared `IToolHitTarget` receiver; movement and death fade use Resource parameters. DEBUG is unchanged.
