# ENVIRONMENT / Seasons

**Status: planning only.** No production season controller, F9 year slider, seasonal tree shader or calendar/year system is implemented yet.

**Codex phase handoff: [SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md](SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md)**:
- SE0: one shared season-preview state plus a live F9 Spring/Summer/Autumn/Winter year-position slider.
- SE1: healthy Oak leaves turn autumn yellow/orange/red with data-driven palette and no-mask fallback.
- SE2: protect existing wind, wetness, snow, fire, shadows, P3 chunks and P4 campaigns.
- SE3: optional per-artwork exact foliage-mask Resource input for the user's future **external Avalonia Sprite Studio** (do not build Studio in this pass).
- SE4: a true calendar, weather/temperature season effects, leaf fall and versioned save integration (deferred).

State belongs to ENVIRONMENT/Seasons; generic foliage colouring belongs under WORLD/Content/Vegetation/Shared/Seasonal; species masks and colour palettes belong to WORLD/Content/Vegetation/Trees; F9 controls belong to DEBUG/UI/Environment. Keep one existing game clock; Time/Calendar is currently a placeholder.

This plan does not create any runtime scripts/resources. See [Definitions](Definitions/README.md) for historical longer-term season/climate ideas.
