# B1 layered snowfall and ground spindrift

Implemented on base 330cbfb; awaiting native/Windows visual validation. Only B1: no haze/daylight/visibility/frost, rain migration, weather scheduler or gameplay effects.

## Integration / configuration

Normal EnvironmentRuntime now passes its existing WorldSnow and WorldWeather to WorldBlizzard. At positive resolved intensity it creates one SnowfallVisualLayers helper; ordinary snow retains its original emitter and behavior. The helper borrows WorldSnow.FallingTexture, avoiding a second flake-atlas generator/cache. Three GPU storm layers partition ExtraFlakes: 40% far, 18% near, remainder mid. Far is smaller/softer, near is larger/faster; normal flakes supply the existing main layer. GPU lifetime alpha fades prevent hard wisp/flake expiration. Spindrift uses one generated feathered wisp texture and a separate low-count emitter below actors/above ground (Z −4); it is presentational, with no terrain/obstacle queries or collision.

BlizzardProfile owns changed-only toggles LayeredSnow/Spindrift, ExtraFlakes 0–1300, DriftParticles 0–128, GustStrength 0–1, GustSeconds 1–15. Defaults: 1300 extra, 64 drift, 0.45 gust, 5s cadence. F9 exposes independent switches, budgets and gust strength under Snow → Blizzard. Existing snowfall slider maps 1–3 to resolved severity 0–1. No second severity control or changes to global wind/rain/coverage. Profile remains cloned and runtime-loaded outside the save resource graph.

A bounded mixed sine waveform varies visual gusts. Nonzero wind keeps its sign; calm wind gets gentle alternating lateral flurries. Near-horizontal motion increases with storm severity. No shared WindController writes. Emitter regions follow the actual weather viewport/camera world rectangle, with modest spawn margins and separate worst-case travel cull margins. World-space particles preserve their path while the camera moves. No screen-space HUD overlay is involved.

## Performance/lifecycle

Budget: normal 1300 + storm at most 1300 = 2600 flakes, plus default 64/max 128 spindrift wisps. This is a provisional quality budget, not a measured FPS promise. Counts apply once when toggles/budgets change; intensity adjusts amount/opacity without rebuilding textures/materials. Rate transitions dispatch changes via B0. At a steady storm, the severity controller sleeps; only the active helper follows the view and updates gust motion at most 10Hz. Unchanged motion vectors and unchanged bounds skip setters. GPU particle motion/time remains active while visible.

Zero storm removes the optional helper immediately, including its emitters; ordinary snowfall survives in its existing range. Snow/Falling snow OFF or rate zero also frees ordinary snowfall through its original owner and clears storm visuals. Re-enable allocates fresh finite helpers. No storm GPU nodes, ground wisps or storm callbacks when never enabled/after zero. Toggling a layer off removes its emitter; rare budget/toggle changes rebuild the small optional emitter set. World unload uses the same shutdown path. No global caches, chunk registrations, full-world scans or per-flake CPU nodes.

## Brief checks performed

- Source review of normal World composition, B0 change dispatch, snowfall creation/free, existing shared wind and F9 controls.
- Checked every integer budget 0–1300 partitions exactly and total flakes never exceed 2600.
- Checked authored maximum signed gust speed × lifetime for all layers fits 1400 horizontal cull margin.
- git diff --check passed.
- Neither dotnet nor Godot is available here. Build/import, runtime toggles, GPU appearance, camera traversal and target-PC performance were not run or claimed passed. No extensive test suite added/run, following CodePreferences.

## Local acceptance

Open SCENES/World/World.tscn → F6, F9 → Environment → Snow. Enable snow/falling; compare 0.65/1/2/3, allowing the 3s transition. At 1 expect original snowfall; above 1 expect added depth flakes and ground wisps. Toggle each layer, vary budgets, then return to 1/0 and rapidly re-enable. Test wind −3/0/+3, camera movement/zoom/resize at 1920×1080. Check no clipped sprites/visible emitter rectangles, readable player and correct left/right drift. B2 darkening/haze is not expected. At low zoom/very large views density may need visual tuning within the bounded budget.

Initial build if desired: dotnet build -p:NuGetAudit=false, then normal Godot .NET import and F6. Existing ValidateBlizzardState/ValidateSnowSleeping fixtures can be rerun locally if lifecycle behavior looks wrong; no headless test proves GPU quality. Report graphics and frame pacing before increasing particle budgets or proceeding to B2.
