# World persistence — P4 awaiting local validation

`WorldBlueprintManifest` retains the P1/P2 creation API used by existing fixtures. Streamed runtime manifests use schema 4 (legacy/no generated water), schema 5 (WG2 lakes) or schema 6 (WG3 rivers), including exact macro region/biome values plus P2 feature snapshots and P3 local-generation metadata. `WorldBlueprintRestore` validates and reconstructs the exact maps/reservations from these values and verifies hashes; Continue never reruns the macro or feature planners.

`WorldChunkStream` imports changed-object records before the first chunk is created. Removed props never materialize; burn progress restores through the existing shared fire lifecycle. `CaptureChanges` captures current loaded props and retained unloaded deltas into detached per-chunk records. Only changed states persist, scoped by WorldId/surface/chunk and stable candidate ID. Healthy visited trees do not accumulate save records. The coordinator in SAVE owns profile selection, disk IO and whole-checkpoint consistency.

F6 without a campaign launch remains a disposable preview identity; only the existing menu's New Campaign/Continue flow enables durable saves. Future caves/populations must extend the same ownership, not create global seed-keyed caches. See SAVE/README.md and SAVE/P4_Copilot_Handoff.md. No local compilation or Godot tests have been run for this P4 pass.

## WG1 optional water metadata

Schema 4 optionally stores `WaterPlanSnapshot` (water planner version, seed, hash, detached body IDs/liquid IDs/polygons). CampaignStore validates it on save/load. Missing data is a legacy empty plan; Continue never invokes the water planner. Existing scene/Resource text and content fingerprints are unchanged by this pass. WG1 metadata does not affect scenery or terrain. See [WG0/WG1](../../Generation/Liquids/WG0_WG1_Validation.md) before extending this to WG2 gameplay.

## WG2 playable-water worlds

Blueprint schema 5 marks new generated-water gameplay and requires a nonempty validated Water snapshot. Schema 4 (including WG1 metadata) stays legacy and never materializes generated water on Continue. No-water new worlds remain schema 4. Older executables reject schema 5. See [WG2 compatibility and checks](../../Liquids/WG2_Validation.md).

## WG3 river worlds

Blueprint schema 6 carries water-plan version 2: frozen polygon plus explicit source-to-terminal centerline, widths and endpoint kinds. Version-1 lake plans/hash layout and schema-4/5 worlds remain unchanged. Version-2 metadata is rejected under older schema headers, and Continue never invokes the river planner. See [WG3](../../Generation/Liquids/Rivers/WG3_Validation.md).

Smooth river geometry uses blueprint schema 7 / water version 3, with up to 1000 centerline samples and 2048 shoreline vertices per river (lakes remain at 64). Schema 6/version 2 retains the original 24/64 bounds and exact hash layout; supported older saves restore without smoothing/rerouting. New dense data rejects older schema/version headers.
