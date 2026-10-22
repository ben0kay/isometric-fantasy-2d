# World persistence

P1 supplies `WorldBlueprintManifest`: a detached, serializable contract for profile/run/world identity and frozen coarse biome ownership. `WorldScene.Blueprint` currently belongs to a disposable preview session. Equal seeds get equal map hashes but separate WorldIds. No player profile selection, disk saving/loading, delta storage or save migrations are implemented.

Future SAVE/Profiles and SAVE/WorldState own selection and durable coordination; WORLD owns geographical state and changed objects. Use WorldId/layer/chunk keys, never a global seed-keyed mutable cache. See `WORLD/Generation/World/P0_P1_Audit.md` and sections 8/8A of Generation_Implementation.md.
