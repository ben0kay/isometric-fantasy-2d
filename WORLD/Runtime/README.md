# World runtime

WorldScene now composes the P3 finite streamed world by default: seeded macro map/reservations, protected start, finite camera/walls, local chunk ground/scenery and the existing player/pond/environment/fire/skeleton/occlusion systems. Stream owns world-scoped chunk lifecycle and touched session deltas. F7 exposes map and chunk status.

Disable WorldMasterConfig.Streaming for the preserved adjacent-biome compatibility preview. Persistent profiles/Continue, disk deltas and A-Life remain later passes. See Streaming/README.md for implementation details, configuration and validation.
