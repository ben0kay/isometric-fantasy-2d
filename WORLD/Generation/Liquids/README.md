# WORLD/Generation/Liquids

**WG0 audit complete; WG1 macro pond/lake planning implemented.** Generation owns deterministic liquid placement and frozen geometry; `WORLD/Liquids/` owns rendering and interactions. WG2 presentation, rivers and carving are still planned.

`WaterGenerationMasterConfig` is an optional Resource on `WorldGenerationMasterConfig.Water` (null/off by default). `WaterBodyPlanner` consumes P1 ownership and P2 permissions once, plus a fixed-pond exclusion. `WaterBodyPlan` holds immutable bounded geometry, lookup queries, exact hashes and detached save snapshots. `BiomeHydrologyProfile` supplies optional biome permissions/weights. Missing saved data means empty water, never reroll on Continue.

Try `TESTS/Water/WaterPlanningPractice.tscn` with F6→F7 for three blue planned lake footprints; these are metadata only until WG2. No water/scenery/navigation/ground changes are implied by the overlay.

Read [WG0 decisions, WG1 settings, validation and WG2 handoff](WG0_WG1_Validation.md) and [phase plan](WATER_GENERATION_PLAN.md). Preserve the authored pond and DEBUG prototype. No speculative river/carving directories or per-biome managers.
