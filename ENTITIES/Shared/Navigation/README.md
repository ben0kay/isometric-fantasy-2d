# Shared entity navigation

`StaticObstacleNavigation.cs` provides one reusable coarse `AStarGrid2D` made from **world-transformed static collision footprints**, including procedurally placed trees and rocks. It is built only once, after scenery generation, and shared with compatible actors through `IWorldNavigationAgent`.

It marks padded obstacle cells, blocks diagonal corner cutting, and searches nearby free cells for blocked spawn/target locations. The currently implemented Skeleton Basic uses it for periodically updated chase paths, rather than calculating A* every frame.

**Limitations:** Only static bodies and polygon/rectangle footprint children are currently supported. It does not dynamically rebuild after terrain changes, handle swimming/flying or stream infinite worlds. Complex/dense obstacle layouts may remain unreachable, in which case agents stop instead of trying to walk through colliders. Distant groups of enemies should eventually share one grid per active region.

The separate debug navigation test remains untouched.

Smooth river support: polygon rasterization computes interior crossings once per grid row and tests padding against only nearby row edges. This handles dense river banks without full-polygon distance scans per grid cell; the same path remains shared with scenery polygons. Dense-shore equivalence, lake and melee regressions passed; target-PC nav rebuild frame times remain unprofiled.
