# Production environment controls
EnvironmentDebugPanel.tscn is an optional sibling in World.tscn. It binds after environment initialization. **F9** hides/shows it; section headers collapse categories and the panel scrolls. Widgets do not retain arrow-key focus.

Controls: time scrub/pause/day duration, signed wind, colour grade/soft shadows/cloud/torch comparison, atmosphere switches/haze density, rain/overcast/ground splashes/lightning, manual flashes/strikes, master snow/coverage/flakes/rate/automatic accumulation-melt.

Controls only issue commands or edit private runtime profile values; they do not create simulation nodes or save assets. Restart restores authored settings. Remove the panel instance and its external-resource entry to exclude tooling entirely; production ENVIRONMENT has no DEBUG dependency. EnvironmentPath can bind another compatible production environment node.

World diagnostics now occupy a separate scrollable right-side column owned by this same removable panel. F9 hides/shows both columns together. FPS/streaming/sun counters refresh at 4 Hz only while visible; world/biome/reservation details and the culling comparison toggle moved here from F7. F7 now controls only the map.

Validation: C# compiled with Godot source generators; a headless runtime UI smoke check verified right anchoring, F9 hiding/restoring both columns, map-only F7, and independent map/diagnostic visibility.
