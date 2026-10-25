# Shared harvesting

`HarvestProfile` authors accepted tool capability, required work and reward table. `Harvestable` owns per-source progress and once-only completion, without _Process/_PhysicsProcess. Work enters only through an enriched confirmed contact; the owner's completion callback must reserve rewards before the final work commits. Denial leaves the source retryable with its original chosen quantities.

Tree-only fall presentation lives under WORLD/Content/Vegetation/Trees/Shared; generic rewards and pickups under [SYSTEMS/Drops](../Drops/README.md). Actual inventory storage stays under SYSTEMS/Inventory. No per-species harvest scripts or duplicated inventory logic.

**H0–H4 implemented; Windows visual/performance acceptance pending. H5 planned.** Normal World and campaigns now use the real harvesting/inventory/drop loop. [H4 integration and checks](H4_World_Validation.md). [Plan](HARVESTING_IMPLEMENTATION_PLAN.md) · [H0 audit](H0_Audit.md) · [implementation/test evidence](H0_H3_Validation.md).

F6 `TESTS/Harvesting/HarvestingPractice.tscn`: LMB axe ×4, 3–5 logs hop/scatter, E collects nearest settled item, I shows real backpack counts. MMB/sword do not chop. Oak and mirrored Blossom reuse the same scene/profile; a generic work target and manual drop button exercise reuse. No stump, regrowth, falling damage, campaign persistence or world streaming changes.

`WorldHarvestRuntime` binds shared systems to real Player, chunk streaming and campaign snapshots. `DefaultTreeHarvest.tres` supplies the production four-work axe profile and 3–5 canonical Wooden Logs. Species Resources opt in; no species-specific harvesting script. Partial work, felled source tombstones, uncollected landing-owned drops and carried inventory persist together. F6 World is disposable; campaigns save via F8/Esc. See H4 validation for controls and migration boundaries.
