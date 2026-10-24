# WORLD/Generation

Reusable world-generation **algorithms**, not an asset dump. Recommended pipeline: world geography → terrain/water → biome selection → natural content placement → locations → entity populations → persistence restore. Use seeds and placement rules for deterministic revisit behaviour.

**Implemented:** `World/` owns the P1 finite macro biome map and its configuration; `Placement/` owns the existing preview startup scatter. `Liquids/` now provides opt-in WG1 frozen pond/lake planning (rendering is WG2); other unimplemented domains remain scaffolding. See [macro generation](World/README.md).
