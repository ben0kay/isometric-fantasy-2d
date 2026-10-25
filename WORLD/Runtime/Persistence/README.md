# World persistence

`WorldBlueprintManifest` retains the P1/P2 creation API used by existing fixtures. New streamed worlds use schema 9 (P5c ocean/non-land); schema 4–8 manifests remain supported legacy outcomes. All retain exact macro regions/biomes, P2 features and P3 local metadata. Older WG sections below describe their original creation policies, not the current new-world default. `WorldBlueprintRestore` validates and reconstructs the exact maps/reservations from these values and verifies hashes; Continue never reruns the macro or feature planners.

`WorldChunkStream` imports changed-object records before the first chunk is created. Removed props never materialize; burn progress restores through the existing shared fire lifecycle. `CaptureChanges` captures current loaded props and retained unloaded deltas into detached per-chunk records. Only changed states persist, scoped by WorldId/surface/chunk and stable candidate ID. Healthy visited trees do not accumulate save records. The coordinator in SAVE owns profile selection, disk IO and whole-checkpoint consistency.

F6 without a campaign launch remains a disposable preview identity; only the existing menu's New Campaign/Continue flow enables durable saves. Future caves/populations must extend the same ownership, not create global seed-keyed caches. See SAVE/README.md and SAVE/P4_Copilot_Handoff.md. No local compilation or Godot tests have been run for this P4 pass.

## WG1 optional water metadata

Schema 4 optionally stores `WaterPlanSnapshot` (water planner version, seed, hash, detached body IDs/liquid IDs/polygons). CampaignStore validates it on save/load. Missing data is a legacy empty plan; Continue never invokes the water planner. Existing scene/Resource text and content fingerprints are unchanged by this pass. WG1 metadata does not affect scenery or terrain. See [WG0/WG1](../../Generation/Liquids/WG0_WG1_Validation.md) before extending this to WG2 gameplay.

## WG2 playable-water worlds

Blueprint schema 5 marks new generated-water gameplay and requires a nonempty validated Water snapshot. Schema 4 (including WG1 metadata) stays legacy and never materializes generated water on Continue. No-water new worlds remain schema 4. Older executables reject schema 5. See [WG2 compatibility and checks](../../Liquids/WG2_Validation.md).

## WG3 river worlds

Blueprint schema 6 carries water-plan version 2: frozen polygon plus explicit source-to-terminal centerline, widths and endpoint kinds. Version-1 lake plans/hash layout and schema-4/5 worlds remain unchanged. Version-2 metadata is rejected under older schema headers, and Continue never invokes the river planner. See [WG3](../../Generation/Liquids/Rivers/WG3_Validation.md).

Smooth river geometry uses blueprint schema 7 / water version 3, with up to 1000 centerline samples and 2048 shoreline vertices per river (lakes remain at 64). Schema 6/version 2 retains the original 24/64 bounds and exact hash layout; supported older saves restore without smoothing/rerouting. New dense data rejects older schema/version headers.

## P5a/P5b geography-first worlds

New worlds use schema 8 / geography version 1 / macro generator version 2, including empty-water worlds. Frozen Water remains lake version 1 or smooth-river version 3. Restore hashes the map using its saved generator version; legacy versions are not relabeled. Continue restores water and map without reapplying current biome eligibility, even if new-world water is disabled. The geography wrapper is rebuilt from frozen water only; no duplicate raster mask is saved. Schema 4–7 retain exact prior outcomes. See [contract and validation](../../Generation/World/Geography/README.md).

## P5c oceans and expanded bounds

Schema 9 requires geography version 2 / macro generator version 3. Ocean-bearing WaterPlan v4 hashes explicit ocean flags; v1/v3 remain valid for ocean-free new worlds. Only macro v3 accepts region cell -1 for non-land; region counts exclude those cells. Restore preserves exact bounds/cell size/map/water, including old schema-8 / macro-v2 worlds. The exact new preset is normalized by the existing narrowly gated creation-default fingerprint rule; old campaigns are not enlarged or given oceans. [Validation](../../Generation/Liquids/Oceans/P5C_Validation.md).
