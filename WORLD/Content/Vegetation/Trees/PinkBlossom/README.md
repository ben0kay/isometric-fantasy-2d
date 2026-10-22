# PinkBlossom

PinkBlossom.tres is canonical species data for the shared ../Shared/Tree.tscn. Its inline TreeVisualVariant configures the existing sprite, root anchor, scale, footprint and placement bounds. The former species scene is removed; biomes continue referencing the same .tres path.

Hit feedback, wind, snow, shadows and optional burning use shared components. FoliageColour in PinkBlossom.tres supplies pink petals for ambient and hit leaves. PinkBlossomFireProfile.tres enables the shared burn lifecycle and canopy flames without charred artwork; the healthy image remains after burning until artwork is supplied.

For new species/variants, edit Resources and biome references only. See ../README.md.
