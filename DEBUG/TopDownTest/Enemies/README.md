# Skeleton chase / axe damage prototype

**Debug only.** In DEBUG/TopDownTest/TopDownVisualTest.tscn (F6, or F5 for project main), one small stationary-art skeleton appears north of the starting clearing on the existing dirt trail. Approach within its detection radius and it follows you. It does not attack or damage the player.

- Move with **arrow keys**; **left-click** to swing the currently equipped axe toward the skeleton. Existing axe aim, reach, 65-degree sector, swing timing, and nearest-target rules remain unchanged.
- Skeleton has **3 HP** by default. Each successful aimed swing removes one HP and briefly flashes it red. On the third hit it fades out and is freed; no respawn/loot or attack animation.
- Existing tree hits are unchanged. If a tree is the nearest eligible aim/range target, the tree receives the hit instead.
- The SVG is temporary static 32x48 art; its root is foot-anchored for Y sorting. Its physics circle prevents walking through static props and the player.
- The skeleton uses a **shared static AStarGrid2D** built once after Actors scatters trees and rocks. It rasterizes their actual transformed collision polygons, plus the existing torch's rectangle, with clearance so the skeleton can route around them. Diagonals cannot cut blocked corners. It only repaths every 0.45s while chasing; no per-frame world scan or navigation baking. Dynamic obstacles, fully complex pathfinding, water avoidance, and groups of enemies are **not** covered.
- Because routes are a 24-pixel grid approximation, very narrow gaps or unusually dense tree arrangements may still be impassable. When no path exists the skeleton stops instead of trying to walk through collisions. This test uses only one skeleton.
- All tuning is in **DefaultEnemyConfig.tres**: spawn position, enabled flag, HP, detection radius, speed, stopping distance, repath interval, navigation grid cell size, collider clearance and flash/fade times.

## Files

- EnemyTest.tscn / DebugEnemyTest.cs: self-contained test instance/controller, creates shared navigation and spawns one skeleton.
- DebugEnemyNavigation.cs: builds a lightweight grid from already-generated scenery collision shapes and produces paths.
- SkeletonTest.tscn / DebugSkeleton.cs: temporary SVG sprite, collision, chase, IWeaponHitTarget receiver, health and death feedback.
- EnemyMasterConfig.cs / DefaultEnemyConfig.tres: Inspector-editable settings.
- Artwork/skeleton.svg: temporary unanimated art.

## Removal / limits

Remove the **EnemyTest** root-level instance from TopDownVisualTest.tscn, then delete DEBUG/TopDownTest/Enemies/ to remove the entire feature. The axe, tree feedback, scenery, weather, lighting, caves and production folders are unmodified.

## Manual checks

1. Start F6 and confirm a skeleton appears on the trail north of the player.
2. Approach until it follows. Lead it around several trees or rocks and see it choose a route rather than remain pressed against an obstacle.
3. Swing from outside axe reach and in the wrong direction: HP should not change.
4. Land three close, aimed swings; check red feedback and complete disappearance on the third.
5. Verify hitting trees still produces wobble/leaves. Try a different world seed and confirm no C# exceptions.

**Validation status:** Implementation inspected against existing C# and scene references. No local Godot/.NET runner is available in this environment, so compilation and in-engine playtesting are still required on the user's machine.
