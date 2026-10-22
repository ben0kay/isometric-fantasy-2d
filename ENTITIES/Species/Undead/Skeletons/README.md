# Skeletons

Basic Skeleton lives in `Basic/SkeletonBasic.tres` and `Basic/Sprites/skeleton_basic.svg`. All skeletons can now instantiate the shared `ENTITIES/Shared/Entity.tscn` and use `EntityDefinition` Resources; common hits/health/death live in `ENTITIES/Shared/Entity.cs`, optional chase in `ENTITIES/Shared/AI/GroundChase.cs`. `Shared/` is reserved for *skeleton-only* reusable assets. DEBUG isolated.
