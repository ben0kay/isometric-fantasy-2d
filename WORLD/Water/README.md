# WORLD/Water — legacy water-only scaffolding

**Status: not implemented; preserved during the new liquid-system foundation pass.** This earlier folder reserved `Lakes/`, `Rivers/` and `Shorelines/` for water-specific content.

The selected canonical architecture for **all liquids**, including water, lava and poisonous pools, is now [WORLD/Liquids/](../Liquids/README.md), with individual type data under `WORLD/Liquids/Definitions/` and shared regions/rendering/interactions under `WORLD/Liquids/`. Future shape generation belongs to [WORLD/Generation/Liquids/](../Generation/Liquids/README.md).

**Do not implement a second overlapping Water pond framework here.** This folder and its empty scaffolding remain until a separate cleanup/migration job; nothing is moved/deleted as part of this foundation.
