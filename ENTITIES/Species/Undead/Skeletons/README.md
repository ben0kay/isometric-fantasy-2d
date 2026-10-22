# Skeletons — production species

**Skeleton Basic** is the first production skeleton variant, adapted from the DEBUG prototype without importing DEBUG files.

## Files

- [Variants/Basic/SkeletonBasic.tscn](Variants/Basic/SkeletonBasic.tscn): reusable `CharacterBody2D` scene with foot collision and a static skeleton sprite.
- `Variants/Basic/SkeletonBasic.cs`: simple target detection, obstacle-aware following, health, hit flash and death fade.
- `Variants/Basic/SkeletonBasicConfig.cs` and `SkeletonBasic.tres`: Inspector-configurable stats.
- `Visuals/Sprites/skeleton_basic.svg`: self-contained copy of the temporary DEBUG art.
- `ENTITIES/Shared/Navigation/StaticObstacleNavigation.cs`: reusable static collision grid for world-generated trees and rocks.

## Where it appears

`SCENES/World/World.tscn` now contains optional `EntityPlacement` that spawns one Skeleton Basic after the random scenery has been placed. The variant is a regular production asset and may eventually be referenced by biome population definitions. This particular single-skeleton placement is intentionally temporary.

**Behaviour:** Detects the player within 300 pixels, follows on a periodically updated A* route, stops near the player, and does NOT attack or hurt them. It has 3 HP and `TakeDamage(int)` with a red flash/death fade, but **the production player has no axe/attack implementation yet**, so the health receiver is not connected to user input. The debug axe remains isolated.

**Limitations:** Static obstacles only (no live navigation rebuild after chopping or construction), potentially unreachable tight passageways, no creature population logic, loot, respawns, animations or persistence. Water avoidance is not part of this first pass.

Later skeleton warriors, archers etc. can be variants of the same species, with data-driven roles and equipment rather than independent combat engines.
