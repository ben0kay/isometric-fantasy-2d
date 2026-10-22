# World runtime

WorldScene now composes the P3 finite streamed world by default: seeded macro map/reservations, protected start, finite camera/walls, local chunk ground/scenery and the existing player/pond/environment/fire/skeleton/occlusion systems. Stream owns world-scoped chunk lifecycle and touched session deltas. F7 exposes map and chunk status.

Disable WorldMasterConfig.Streaming for the preserved adjacent-biome compatibility preview. Persistent profiles/Continue, disk deltas and A-Life remain later passes. See Streaming/README.md for implementation details, configuration and validation.

## SO2 ownership — awaiting local validation
WorldScene caches typed actor/entity/prop/chunk/preview/ground destinations and services once at tree entry. WorldActorSources.Initial traverses marked ownership containers only at initial adapter setup, excluding streamed roots; each chunk keeps its existing independent lifecycle. No registry or per-frame discovery is added. F5 P4 private campaigns and F6 disposable worlds use the same roots. See SCENES/World/SO2_Copilot_Handoff.md for local tests and exact migration paths.
