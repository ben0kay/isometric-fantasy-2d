# SAVE — P4 implemented; automated validation passed, manual acceptance pending

Existing Boot/Main Menu profiles now select durable private campaigns. `ProfileStore` keeps its original GUID/name `user://profiles.cfg` format; it is not replaced by a second registry.

- **Profiles/**: local identity, registry backup/recovery. Each profile selects one active run; starting another requires confirmation and retains the previous run's files.
- **WorldState/**: detached checkpoint records, campaign store, one-shot scene launch and shared save coordinator.
- **Serialization/**: checksummed JSON envelopes, immutable content-addressed blobs, flushed temp files and atomic index replacement with a previous checkpoint backup.
- **Migrations/**: explicit schema/generator compatibility gate. No old durable schema previously shipped; unsupported versions are refused and retained, not silently converted.

`SaveMasterConfig` / `DefaultSaveConfig.tres` configure autosave interval (120 seconds; 0 disables), per-file limit (32 MB), and changed-chunk count limit (32768). No world generation knobs belong here.

## Behaviour

F5 Boot → select/create profile → Main Menu. **New Campaign** makes independent RunId/WorldId and a seed before loading production World. Its first checkpoint activates the run only after successful storage. **Continue** is enabled only after integrity, ownership, versions and content checks succeed. A recovered backup is reported in the menu. Profile display names never become paths.

In a campaign: **F8** saves; **Esc** opens Resume / Save & Main Menu / Save & Quit. OS window close also waits for a final successful checkpoint. Save errors keep the game open and offer retry through F8/Esc. Autosave uses one pausable timer; overlapping requests coalesce. Save-and-leave waits for any existing write and then captures a fresh final snapshot while paused. Force-kill/power loss can lose changes since the last committed checkpoint; a normal close waits for the write.

F6 direct World remains disposable and does not write campaign files. The compatibility preview and existing DEBUG prototypes are preserved.

## Saved state and boundaries

Each checkpoint preserves ProfileId → RunId → WorldId, fixed seed, exact macro cells/regions and feature reservations, generator/schema versions, player surface coordinates/layer, hour/cycle/pause, removed generated scenery, tree burn progress, and the one authored test skeleton's existence/health/position. Detailed scenery remains generated locally; it is not serialized as Nodes. Fire stays paused while unloaded, as in P3. Inactive profiles do not advance by elapsed wall time.

Inventory, player health/needs, equipment, caves, seasons/weather progression and A-Life do not exist as finished gameplay systems; this pass does not fabricate them. Future owners must supply/restore their real state through the coordinator. Current supported layer is `surface`; unknown layers fail clearly. Weather/debug effect sliders reset; the world clock persists. No permadeath deletion, shared worlds, multiple-slot UI or archive browser is implemented.

A strict text-resource/scene dependency fingerprint protects local deterministic scenery when authored definitions/settings change; random-new-world seed policy is excluded. Continue refuses incompatible content rather than shifting trees or resurrecting removed objects. Algorithms require explicit generator-version changes. Automatic content migration and exported-build resource fingerprinting require a later pass; this trial targets the source Godot project.

## Storage and performance

`user://saves/<ProfileId>/active.json` points to `runs/<RunId>/<WorldId>/checkpoint.json`. Per-run `blobs/<SHA256>.json` hold immutable macro and chunk values; unchanged blobs are reused. The index is committed last, so it cannot reference half-written blobs. `.bak` retains the previous committed index/pointer; `.tmp` is never treated as committed. Invalid primaries are quarantined before a later save so a valid backup is not overwritten by corrupt data. Checksums detect accidental corruption; they are not authentication.

Only changed props accumulate as world deltas. At a save boundary, the main thread inspects the bounded loaded chunk ring and copies changed records; no per-tree save polling, no whole-world scene scan and no new `_Process`/`_PhysicsProcess` loop. One worker validates/serializes/writes detached data. Initial map/content checks and arrival preload remain synchronous startup work. Snapshot work scales with loaded props and accumulated edits; index serialization/validation scales with touched chunks. This is not a guaranteed zero-hitch budget: measure large touched-world snapshots locally before extending scale.

Archived runs and unreferenced immutable blobs are retained deliberately; automated garbage collection/quota eviction is not implemented. Per-file/count limits fail visibly instead of truncating state. No automatic destructive cleanup runs.

**Validation:** VS Code Copilot reports Godot 4.7.2 Mono import and .NET build successful (0 warnings/errors), P4 separate-process write/read 31/33 checks passed, and P1/P2/P3/scenery regression suites passed on commit `ce6767d1b37b34210b010b743ea2133d95c2ae15`. Full commands and warnings are in [P4_Copilot_Handoff.md](P4_Copilot_Handoff.md). **Manual in-editor save/quit/Continue, profile-switching UX and target-PC visual/performance tests are still outstanding**; automated tests do not prove FPS or graphical correctness.
