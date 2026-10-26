# Desert — placeholder biome

Registered in the normal finite World generation collection for F6/F7 and new campaigns. Ground only: no scenery populations, creatures, deposits, accompanying grass patches or overlays. Stable ID: `biome.desert`.

Edit `Biome.tres` for identity colour, generation weight/size and baseline climate; edit `/WORLD/Terrain/Ground/Surfaces/Profiles/DesertPlaceholder.tres` for the simple granular surface. The existing canonical texture/material are reused with artwork influence zero. Replace the surface or add canonical populations later without creating a biome-specific script.

Climate is inert authoring data until BC1/BC2; this biome currently participates in globally weighted placement. Pale tundra ground is placeholder artwork, not permanent snowpack or live snow simulation (BC3). No new callbacks or per-object simulation; existing chunk streaming owns loading/unloading.

Use disposable F6 World or a fresh campaign. Adding definitions changes content fingerprints; existing saves are not migrated or rerolled. Static references checked; Godot rendering/import and Windows acceptance remain with the user.
