# Liquid Interactions

**Scaffolding only.** Generic, optional responses applied when an entity enters a region: movement multipliers, wading/immersion, periodic damage, resistance/immunity checks, and surface impacts. A water profile can slow the player; a future lava profile could request damage. Actual health/damage, consumables and enchantment logic belong to the appropriate gameplay systems, not to a hardcoded `if lava` in the shared region or shader.

Player immersion visuals should integrate with `ENTITIES/Player/Visuals/Surfaces/` and coexist with production snow burial. Surface rain splashes consume `ENVIRONMENT/Weather/` rain state and an active liquid's visual settings.
