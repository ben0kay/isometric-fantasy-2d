# Shared tree hit response

TreeHitResponse retains the shared tool-hit contract, received-hit count, trunk wobble and VisualTransformChanged shadow notifications. Hit-generated leaves now use [FEEDBACK/Particles](../../../../../../FEEDBACK/Particles/README.md), retaining the existing canvas/coordinates, 24 slots and default appearance. No scene children or per-species scripts/profiles are required.

TreeDefinition owns FoliageColour and HasFoliage; burning clears/suppresses healthy leaves and reset respects HasFoliage. TreeHitConfig owns wobble, LeavesPerHit and an optional shared/embedded LeafParticles override. Ambient wind/weather leaves remain unchanged.

Wobble and shadow notifications stop at wobble completion; the existing callback continues only for active leaves, then sleeps after the final clear redraw. Disable/suspension/unload clears particles and restores art. [C3.3a+b validation](../../../../../../COMBAT/C3_3ab_Validation.md) distinguishes headless evidence from pending Windows appearance/profiling.

H3: ReceiveHarvestHit optionally forwards the accepted swing's snapshotted tool work to a bound Harvestable. ReceiveHit remains visual-only. Nonfinal/wrong-tool contacts retain normal feedback; completed felling disables this receiver and restores the rooted pose before creating the fall proxy. Normal production trees have no harvesting component.
