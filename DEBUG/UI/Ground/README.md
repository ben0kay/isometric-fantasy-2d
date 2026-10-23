# Ground Surfaces — F9 / GS4

The existing EnvironmentDebugPanel hosts Ground Surfaces as its third exclusive category beside Environment and Combat. No new production hotkey, root overlay or diagnostic column. F7 is untouched; right diagnostics remain Environment-only.

Controls: current biome ID/name and base label/profile, base colour variation and fine detail, optional-layer master, separate A/B visibility, global coverage/opacity multipliers (0–2) and Reset Ground Preview. Live slider events change only shared runtime uniforms; no coarse map regeneration or texture upload. Reset restores authored base flags and neutral multipliers. Hiding/collapsing F9 keeps the comparison; leaving the world discards it. Authored-off/unconfigured slots stay off and are clearly labelled/disabled. Existing Oak/Pink have empty overlay arrays.

GroundSurfacePreview is the narrow production-owned API. The UI reads Resource labels but never writes them. Controls are global comparisons across compiled active layers, not per-biome resource edits. No files, saves, world IDs, collision, generation or scenery are changed. The compatibility preview offers base controls only.

Only the visible drawer refreshes current-biome text/widgets (maximum 4Hz); hidden callbacks stop. Widget refresh uses no-signal setters and causes no GPU writes. Labels update when biome changes. FocusMode.None leaves movement keys available. The practice scene reuses this exact panel/API with injected fixture data and its existing test-only F9 handler.

See [GS4 validation and all-phase local checklist](../../../WORLD/Terrain/Ground/Surfaces/GS4_Validation.md). Godot/.NET and Windows acceptance remain pending.
