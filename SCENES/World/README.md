# Production World — streamed sandbox and SO2 ownership

Open `World.tscn` in Godot .NET and press **F6** for a **disposable** streamed production test session (no P4 disk save). **F5** enters Boot/Profile/Main Menu; New Campaign / Continue starts durable private P4 campaigns. See [SAVE](../../SAVE/README.md).

## Current default

`WORLD/DefaultWorldConfig.tres` enables P0/P1 finite macro biomes, P2 reservations and P3 streaming. The current world is **45,000×33,750** with **512-unit chunks**, active radius 2, prefetch 3 and unload 4. Oak Forest and Pink Meadow share canonical Resource-driven content. Fresh disposable F6 sessions use a new seed unless fixed replay is configured. Set `WorldMasterConfig.Streaming.Enabled=false` for the old **3840×1728** adjacent-biome compatibility preview. See [generation](../../WORLD/Generation/World/README.md) and [streaming](../../WORLD/Runtime/Streaming/README.md).

World composes player, authored Skeleton Basic combat practice (shared chase/melee), pond/wading, torches, FIRE1/FIRE2, clock/wind, sun/local shadows, rain/wetness, snow, occlusion and atmosphere. [Shared Entity](../../ENTITIES/Shared/README.md) and [C0/C1 combat](../../COMBAT/COMBAT_IMPLEMENTATION.md) are implemented foundations.

## SO2 scene ownership — implemented

`TerrainRoot` owns ground/terrain; `WorldActorsRoot` is the common Y-sort parent of `PersistentEntitiesRoot` (player and nested DebugActorsRoot), `PersistentPropsRoot` (torches), `ChunkActorsRoot` (streamed props) and `PreviewSceneryRoot` (compatibility scene). `SystemsRoot` owns placement and runtime controllers; `DebugUIRoot` owns removable panels. Typed destinations replace the old mixed `Actors` hierarchy. No second streamer or per-frame source scan was introduced. See [SO2 validation](SO2_Validation.md) and [SO3/SO4 future plan](scene%20organize.md).

## Controls, saves and acceptance

- **F7** macro map/reservations; **F9** environment/performance diagnostics; removable Fire buttons ignite/reset nearby trees. **F8** explicit campaign save, **Esc** campaign Resume / Save & Main Menu / Save & Quit. F6 direct World is unsaved.
- **P4** Continue restores frozen map/features, player position/HP, clock, supported chunk edits/burns and the authored test skeleton. Earlier original P4 functional save/profile checks were **accepted by the user** after Copilot's 31/33 automated tests.
- **SO2** Godot 4.7.2 build/import, scene ownership (60 assertions), SO1→SO2 real disk restore and P4 write/read **39/33** automated checks passed. Newer [C1 validation](../../COMBAT/C1_Validation.md) extended save checks to **43/39**. Counts are revision-specific.
- **Open:** visually verify Y-sort/seams, Windows frame pacing and large-save performance, and perform newer SO2/C1 graphical/UI regression testing. Headless passes are not proof of FPS. See [SO2 manual checklist](SO2_Copilot_Handoff.md) and [streaming evidence](../../WORLD/Runtime/Streaming/README.md).


## AC2 practice actors

F6 now places **five skeletons total** around the player start. Select `WorldActorsRoot/PersistentEntitiesRoot/DebugActorsRoot` and uncheck **Enabled** to disable all test skeletons. Four child markers tune extra spawn offsets; placement adjusts to nearby walkable, separated points. Existing spawned actors can be toggled in the Remote Inspector; after starting disabled, reopen the scene to populate when enabled. Y-sort ownership metadata is preserved. Campaigns retain only the original saved skeleton; the extra four are disposable F6 practice. See [debug root instructions](../../DEBUG/World/README.md) and [AC2 validation handoff](../../ENTITIES/Shared/Movement/AC2_Validation.md).
