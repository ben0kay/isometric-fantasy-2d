# SYSTEMS / WorldInteractions

**Status:** production FIRE1/FIRE2 are wired into the World scene. Other interaction categories remain future work.

This category groups **reusable mechanics that change or react with objects in the world**, without owning the physical objects themselves. An interaction may involve vegetation, structures, entities, weather or magic, but the behaviour should remain reusable across content types.

## Current modules

- [Fire/](Fire/README.md): ignition, burn-state progression, common fire presentation and eventual spread/extinguishing. FIRE1/FIRE2 now run in production; the independent DEBUG test is preserved.

## Potential future categories (not created)

Freezing, electrical effects, corrosion and structural destruction are possibilities, not approved implementations. Only add folders for them once mechanics have a concrete owner.

## Ownership and dependencies

- **SYSTEMS/WorldInteractions:** the reusable action, state and its gameplay outcomes.
- **WORLD/Content:** object definitions, species-specific artwork, grounded colliders, charred/broken variants and object-specific render adapters.
- **ENVIRONMENT:** authoritative weather, lightning, wind, rain, snow and world lighting. Interactions consume them; they must not duplicate the environment simulation.
- **ENTITIES:** creature-specific stats, resistance, injury, health and death; reusable interaction mechanics can request changes through defined interfaces.
- **DEBUG/UI:** optional developer buttons, previews and sliders that can be removed without disabling production systems.

Avoid copying scenes from `DEBUG/TopDownTest/` into production without adapting their paths, unique Godot C# class names and event ownership. Keep one `SYSTEMS/` feature config per substantial mechanic as needed, not a new global controller for every object.

Related: [SYSTEMS overview](../README.md), [WORLD scenery](../../WORLD/README.md), [ENVIRONMENT](../../ENVIRONMENT/README.md).
