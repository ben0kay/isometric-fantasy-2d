# WORLD Runtime — P3 stream, P4 persistent state

**Production default:** `WorldScene` composes P0/P1 macro map, P2 reservations and P3 nearby chunk ground/scenery/navigation. `WorldChunkStream` registers wind/weather/shadow/fire/occlusion adaptors per chunk and unregisters at unload. Edited/burnt objects survive local unload/reload as session deltas. See [Streaming](Streaming/README.md) and [P0–P6 phase status](../Generation_Implementation.md).

**P4 durable private campaigns and Continue are implemented** under [SAVE](../../SAVE/README.md): profiles, run/world identity, restored player/clock/skeleton and supported chunk edits. F6 direct World sessions remain disposable; F5 starts Boot/Main Menu. Do not create duplicate save coordinators or per-tree polling.

**SO2 implemented and headless-tested:** `WorldScene` caches explicit typed persistent actor/prop, streamed chunk, preview and terrain destinations; streamed effects continue lifecycle registration. Actual older SO1 disk checkpoints restored correctly. See [SO2 evidence](../../SCENES/World/SO2_Validation.md). Rendered Y-sort and Windows target-PC frame times remain unverified; SO3/SO4 are future design, not code. P5 features and P6 offscreen A-Life are planned.
