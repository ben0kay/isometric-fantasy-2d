# Liquid Surfaces

**Scaffolding only.** Reusable appearance rendering, liquid-specific shader compositions and inspector-editable surface profiles live here. Water waves/ripples/wet shore, lava slow molten flow/glow, and future viscous or toxic surfaces may share helper functions but need not share identical compositions. Definition data chooses the profile/shader; no biome or liquid-ID branching in general-purpose renderer.

Initial DEBUG source: `DEBUG/TopDownTest/Liquids/Shaders/`. Inputs may include the existing production Wind/Weather state. Avoid per-pixel CPU shoreline calculations and redundant per-frame mesh rebuilds.
