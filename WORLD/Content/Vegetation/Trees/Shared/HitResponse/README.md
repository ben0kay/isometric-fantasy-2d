# Shared tree hit response

TreeHitResponse handles bounded sprite wobble and leaf bursts for all configured tree species. It reuses TreeDefinition.FoliageColour/HasFoliage, also consumed by ambient falling leaves. Burning/burnt trees suppress healthy foliage; reset respects HasFoliage.

Default wobble/burst tuning comes from TreeHitConfig code defaults. No separate hit .tres is required. A species can embed a HitConfig subresource inside its existing tree .tres when timing/count overrides are needed. Player input stays in PlayerInput.cs.
