# Shared tree runtime

Tree.tscn is the one internal production template. Tree.cs applies TreeDefinition + a seeded VisualVariants index before child readiness; external content tooling authors Resources rather than scenes. TreeVisualVariant owns rooted artwork/collision/bounds and optional per-artwork wind/fire overrides. See ../README.md for the full Resource contract.

HitResponse and Fire folders own their shared responses. ENVIRONMENT still owns authoritative wind, snow, lighting and shadow simulation. These nodes are configured before the world's environment/fire startup caches are built; no tree idle wind processor is added.

SE1 adds optional TreeDefinition.SeasonalFoliage and TreeVisualVariant.FoliageMask. The shared [seasonal adapter](../../Shared/Seasonal/README.md) binds these after wind; no per-tree season processing or duplicate scene is added.
