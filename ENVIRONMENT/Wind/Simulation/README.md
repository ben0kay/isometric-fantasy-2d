# ENVIRONMENT / Wind / Simulation

**Status: placeholders only — no runtime simulation.**

This folder is the future **single source of truth** for world wind conditions.

- `WindController.cs`: empty Node shell for the eventual centralized wind calculation, smoothing, gusts and region-aware sampling. Nothing is attached to a Godot scene.
- `WindState.cs`: empty, immutable-struct placeholder for a read-only snapshot that consumers can query. Future fields may include normalized 2D direction, speed, gusts and velocity.
- `../WindMasterConfig.cs` / `../DefaultWindConfig.tres`: reserved Inspector config resource (no gameplay settings yet).

## Important rules

- Wind direction and nonnegative wind speed must be **separate**, unlike the debug -3..+3 left/right slider.
- Rain, snow, trees, grass, leaves, drifting haze and future liquid ripples all read the same current wind state.
- Visual systems may have their own sensitivity, bend, response time or emission rate. They must not overwrite global wind to tune themselves.
- The state should support changes without teleporting existing leaves, smoke, clouds or other particles; integrated travel belongs to the appropriate consumer.
- Later, handle season, regional weather and shelter modifiers in a planned way. Start with global wind and expand only when necessary.
- Prefer bounded updates and deterministic configuration where useful. Do not create per-tree wind calculations.

Working debug reference: `DEBUG/TopDownTest/Weather/DebugWeather.cs` at repository root. **Do not promote or duplicate it automatically.**
