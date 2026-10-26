# Pink Meadow
Biome.tres owns the pink surface reference and inline populations: 36 pink blossom trees and 16 shared boulders. Decorative grass is absent, matching DEBUG. Canonical tree art/scene/collision live under WORLD/Content/Vegetation/Trees/PinkBlossom; rocks reuse WORLD/Content/Rocks.

The biome sits east of the layout's curved transition. It reuses the existing shared clock, wind, shadows, weather, snow and atmosphere, as in DEBUG. Per-region environment profile switching/blending is not part of this pass; the primary biome supplies the current world-wide environment profile.

BC2: `PlacementClimate.tres` now supplies optional macro-generation affinity independently of Climate baselines. It is consumed only when a world climate plan is present; null world climate uses the legacy placement path. New generation fingerprints/schema require F6/new campaign. See `WORLD/Generation/Authoring/BC2_Validation.md`.
