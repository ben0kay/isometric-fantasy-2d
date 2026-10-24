# WORLD/Generation/Liquids

**WG0 audit complete; WG1 macro pond/lake planning implemented.** Generation owns deterministic liquid placement and frozen geometry; `WORLD/Liquids/` owns rendering and interactions. [WG2 presentation](../../Liquids/WG2_Validation.md) is now implemented; [WG3 rivers](Rivers/README.md) are implemented; carving remains planned.

`WaterGenerationMasterConfig` is an optional Resource on `WorldGenerationMasterConfig.Water` (null/off by default). `WaterBodyPlanner` consumes P1 ownership and P2 permissions once, plus a fixed-pond exclusion. `WaterBodyPlan` holds immutable bounded geometry, lookup queries, exact hashes and detached save snapshots. `BiomeHydrologyProfile` supplies optional biome permissions/weights. Missing saved data means empty water, never reroll on Continue.

Try `TESTS/Water/WaterPlanningPractice.tscn` with F6→F7 for three blue planned lake footprints; new schema-5 worlds now render these through WG2; schema-4 worlds retain metadata only. No water/scenery/navigation/ground changes are implied by the overlay.

Read [WG0 decisions, WG1 settings, validation and WG2 handoff](WG0_WG1_Validation.md) and [phase plan](WATER_GENERATION_PLAN.md). Preserve the authored pond and DEBUG prototype. No speculative river/carving directories or per-biome managers.

WG3 adds river settings to the same generation MasterConfig, one RiverPlanner and a frozen PlannedRiverPath contract. It reuses shared water presentation/interactions and preserves lake-only version-1 hashes. New smooth river worlds are schema 7 (water version 3); earlier schema-6 river saves remain supported; old saves never gain rivers. Try `TESTS/Water/RiverPractice.tscn` → F6, R cycles sections, F7 shows the cyan route.
