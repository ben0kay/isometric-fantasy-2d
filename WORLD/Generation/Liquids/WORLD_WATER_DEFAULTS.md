# Generated water enabled in normal World

`DefaultWorldGeneration.tres` now contains inline Water settings: Enabled=true, LakeCount=3, RiverCount=1. Existing smooth-river size/bend/bank defaults apply. `World.tscn` already references this generation config through DefaultWorldConfig; F6 World and F5 → New Campaign now use it directly. F7 shows locations; no player relocation or developer teleport is added. Counts remain bounded requests, with warnings/skips for infeasible optional placement.

No new renderer, processing loop or manager is added. The existing once-per-new-world pipeline plans water, and shared streaming loads/unloads nearby surfaces. Existing campaigns restore exact frozen geography, including worlds with no generated water.

## Compatibility and checks

The exact shipped config source is SHA-gated in `CampaignSession.NormalizeDefaultWaterGenerationSource` and normalized to `SAVE/Migrations/References/WaterDefaultsBefore.txt` for content fingerprinting. Other config edits and dependency paths retain strict checks; previous migration combinations still compose. This is new-world-default compatibility, not a blueprint migration. An independent dependency-graph reconstruction matches the pre-change fingerprint `C178502447412B8D09E31EA2B162DC0D4EF397C885B071D06D7C3BF5CF729AED` exactly.

Direct .NET 8 Roslyn/Godot source-generator compilation passed. Linux Godot 4.5.1 Mono checks: actual `SCENES/World/World.tscn --fixed-fps 60 --quit-after 180` smoke exited 0; `ValidateStreamedRiver.tscn` now uses the real production water config instead of overriding it, **2998 checks**, exit 0, including new and legacy Continue; `ValidateGeographyFirst.tscn` **141772 checks**, exit 0, including exact compatibility and rejection of an unrelated CellSize edit/path. Existing camera interpolation warning only; no runtime errors in these runs. Editor import prepared assets but retains the runner SDK/MSBuild diagnostics; a clean target Godot 4.7.2 build/visual/profile check remains pending. No FPS claim.

Main development check: open World.tscn → F6 → F7 and explore the water locations. Practice scenes remain optional shortcuts. Windows shoreline rendering, streaming frame times and separate-process campaign Continue remain target checks. Future production work should finish its normal World configuration/wiring rather than leave an unmentioned connection task behind.
