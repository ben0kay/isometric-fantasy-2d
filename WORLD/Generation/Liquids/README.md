# WORLD/Generation/Liquids

**Scaffolding only.** Future deterministic seeded liquid-body generation: site choice, pond/lake outlines, river paths, volcanic lava lakes, drainage and water/body placement. Shape/cutout generation outputs compatible region geometry to `WORLD/Liquids/Regions/`; renderers, palettes and gameplay interactions live in `WORLD/Liquids/`.

Suggested future subdivisions only when implementing them: `Ponds/`, `Lakes/`, `Rivers/` and `Carving/`. Actual terrain excavation, banks and transition masks must coordinate with `WORLD/Terrain/`, avoiding a visually masked puddle pretending to be a carved hole.

Current DEBUG pond is a fixed startup polygon overlay, not a true carved lake. Do not add lake-carving algorithms in the first water-promotion pass.
