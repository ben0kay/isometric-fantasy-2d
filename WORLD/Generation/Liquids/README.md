# WORLD/Generation/Liquids

**WG0 audit complete; WG1 macro pond/lake planning implemented.** Generation owns deterministic liquid placement and frozen geometry; `WORLD/Liquids/` owns rendering and interactions. [WG2 presentation](../../Liquids/WG2_Validation.md) is now implemented; [WG3 rivers](Rivers/README.md) are implemented; carving remains planned.

`WaterGenerationMasterConfig` is an optional Resource on `WorldGenerationMasterConfig.Water` (null/off for unconfigured custom configs; the shipped production preset enables three lakes and one river). `WaterBodyPlanner.BuildGeography` consumes seed/bounds and P2 permissions before biome assignment, plus a fixed-pond exclusion. The `Build(config,map,features)` overload is the preserved legacy biome-first baseline. `WaterBodyPlan` holds immutable bounded geometry, lookup queries, exact hashes and detached save snapshots. `BiomeHydrologyProfile` now supplies permissions/preferences to biome assignment around frozen water. Missing saved data means empty water, never reroll on Continue.

Try `TESTS/Water/WaterPlanningPractice.tscn` with F6→F7 for three blue planned lake footprints; new schema-5 worlds now render these through WG2; schema-4 worlds retain metadata only. No water/scenery/navigation/ground changes are implied by the overlay.

Read [WG0 decisions, WG1 settings, validation and WG2 handoff](WG0_WG1_Validation.md) and [phase plan](WATER_GENERATION_PLAN.md). Preserve the authored pond and DEBUG prototype. No speculative river/carving directories or per-biome managers.

WG3 adds river settings to the same generation MasterConfig, one RiverPlanner and a frozen PlannedRiverPath contract. It reuses shared water presentation/interactions and preserves lake-only version-1 hashes. New geography-first worlds are schema 8 (water version 1 for lakes or 3 for rivers); earlier schema 4–7 saves remain supported without new geography. See [P5a/P5b](../World/Geography/README.md). Try `TESTS/Water/RiverPractice.tscn` → F6, R cycles sections, F7 shows the cyan route.
