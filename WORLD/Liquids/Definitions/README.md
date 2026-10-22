# Liquid Definitions

**Scaffolding only.** All individual liquid types live here, each in its own folder, e.g. `Water/Water.tres` and eventually `Lava/Lava.tres` or `Poison/Poison.tres`. A type specifies a stable ID, its renderer/profile, palette, wave/ripple settings, and configurable interaction responses. Reference shared assets/logic rather than duplicate them. Distinct liquid-specific shader families are allowed. Future biome definitions reference these resources; they never own copies of liquid implementation.
