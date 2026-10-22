# World persistence — P4 awaiting local validation

`WorldBlueprintManifest` retains the P1/P2 creation API used by existing fixtures. Streamed runtime manifests now use schema 4, including exact macro region/biome values plus P2 feature snapshots and P3 local-generation metadata. `WorldBlueprintRestore` validates and reconstructs the exact maps/reservations from these values and verifies hashes; Continue never reruns the macro or feature planners.

`WorldChunkStream` imports changed-object records before the first chunk is created. Removed props never materialize; burn progress restores through the existing shared fire lifecycle. `CaptureChanges` captures current loaded props and retained unloaded deltas into detached per-chunk records. Only changed states persist, scoped by WorldId/surface/chunk and stable candidate ID. Healthy visited trees do not accumulate save records. The coordinator in SAVE owns profile selection, disk IO and whole-checkpoint consistency.

F6 without a campaign launch remains a disposable preview identity; only the existing menu's New Campaign/Continue flow enables durable saves. Future caves/populations must extend the same ownership, not create global seed-keyed caches. See SAVE/README.md and SAVE/P4_Copilot_Handoff.md. No local compilation or Godot tests have been run for this P4 pass.
