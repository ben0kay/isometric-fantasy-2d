# WORLD/Content

**Implemented:** shared scenery definitions, Oak tree, boulder and four decorative grass variants. Biomes reference these canonical resources; other content folders remain scaffolding.

Canonical reusable world objects, species and physical props. Each tree/rock/etc. has a single authoritative home here; biome configurations reference it, not copy it. Species definitions may reference multiple sprite variants with per-variant weights, anchors and optional collider overrides. Use stable IDs; never require a new C# class for every asset.

> Other content/terrain systems remain scaffolding; the implemented foundation is described above.
