# Shared entity navigation

`StaticObstacleNavigation.cs` provides one reusable coarse `AStarGrid2D` made from **world-transformed static collision footprints**, including procedurally placed trees and rocks. It is built only once, after scenery generation, and shared with compatible actors through `IWorldNavigationAgent`.

It marks padded obstacle cells, blocks diagonal corner cutting, and searches nearby free cells for blocked spawn/target locations. The currently implemented Skeleton Basic uses it for periodically updated chase paths, rather than calculating A* every frame.

**Limitations:** Only static bodies and polygon/rectangle footprint children are currently supported. It does not dynamically rebuild after terrain changes, handle swimming/flying or stream infinite worlds. Complex/dense obstacle layouts may remain unreachable, in which case agents stop instead of trying to walk through colliders. Distant groups of enemies should eventually share one grid per active region.

The separate debug navigation test remains untouched.
