# SAVE — P4 implemented; original user functional acceptance recorded

Existing Boot/Main Menu profiles now select durable private campaigns. `ProfileStore` keeps its original GUID/name `user://profiles.cfg` format; it is not replaced by a second registry.

- **Profiles/**: local identity, registry backup/recovery. Each profile selects one active run; starting another requires confirmation and retains the previous run's files.
- **WorldState/**: detached checkpoint records, campaign store, one-shot scene launch and shared save coordinator.
- **Serialization/**: checksummed JSON envelopes, immutable content-addressed blobs, flushed temp files and atomic index replacement with a previous checkpoint backup.
- **Migrations/**: explicit schema/generator compatibility gate. C1 schema 2 adds player HP; schema 1 still loads with configured full player HP when the old field is absent. Unsupported versions are refused and retained.

`SaveMasterConfig` / `DefaultSaveConfig.tres` configure autosave interval (120 seconds; 0 disables), per-file limit (32 MB), and changed-chunk count limit (32768). No world generation knobs belong here.

## Behaviour

F5 Boot → select/create profile → Main Menu. **New Campaign** makes independent RunId/WorldId and a seed before loading production World. Its first checkpoint activates the run only after successful storage. **Continue** is enabled only after integrity, ownership, versions and content checks succeed. A recovered backup is reported in the menu. Profile display names never become paths.

In a campaign: **F8** saves; **Esc** opens Resume / Save & Main Menu / Save & Quit. OS window close also waits for a final successful checkpoint. Save errors keep the game open and offer retry through F8/Esc. Autosave uses one pausable timer; overlapping requests coalesce. Save-and-leave waits for any existing write and then captures a fresh final snapshot while paused. Force-kill/power loss can lose changes since the last committed checkpoint; a normal close waits for the write.

F6 direct World remains disposable and does not write campaign files. The compatibility preview and existing DEBUG prototypes are preserved.

## Saved state and boundaries

Each checkpoint preserves ProfileId → RunId → WorldId, fixed seed, exact macro cells/regions and feature reservations, generator/schema versions, player surface coordinates/layer and HP (schema 2), inventory/shortcut/equipment state plus partial harvest work, felled trees and uncollected drops (schema 3), hour/cycle/pause, removed generated scenery, tree burn progress, and the one authored test skeleton's existence/health/position. Detailed scenery remains generated locally; it is not serialized as Nodes. Fire stays paused while unloaded, as in P3. Inactive profiles do not advance by elapsed wall time.

Inventory and current weapon selection now have real Player ownership and schema-3 persistence. Survival needs, caves, seasons/weather progression and A-Life remain future work. Future owners must supply/restore their real state through the coordinator. Current supported layer is `surface`; unknown layers fail clearly. Weather/debug effect sliders reset; the world clock persists. No permadeath deletion, shared worlds, multiple-slot UI or archive browser is implemented.

A strict text-resource/scene dependency fingerprint protects local deterministic scenery when authored definitions/settings change; random-new-world seed policy is excluded. Continue refuses incompatible content rather than shifting trees or resurrecting removed objects. Algorithms require explicit generator-version changes. Automatic content migration and exported-build resource fingerprinting require a later pass; this trial targets the source Godot project.

## Storage and performance

`user://saves/<ProfileId>/active.json` points to `runs/<RunId>/<WorldId>/checkpoint.json`. Per-run `blobs/<SHA256>.json` hold immutable macro and chunk values; unchanged blobs are reused. The index is committed last, so it cannot reference half-written blobs. `.bak` retains the previous committed index/pointer; `.tmp` is never treated as committed. Invalid primaries are quarantined before a later save so a valid backup is not overwritten by corrupt data. Checksums detect accidental corruption; they are not authentication.

Only changed props accumulate as world deltas. At a save boundary, the main thread inspects the bounded loaded chunk ring and copies changed records; no per-tree save polling, no whole-world scene scan and no new `_Process`/`_PhysicsProcess` loop. One worker validates/serializes/writes detached data. Initial map/content checks and arrival preload remain synchronous startup work. Snapshot work scales with loaded props and accumulated edits; index serialization/validation scales with touched chunks. This is not a guaranteed zero-hitch budget: measure large touched-world snapshots locally before extending scale.

Archived runs and unreferenced immutable blobs are retained deliberately; automated garbage collection/quota eviction is not implemented. Per-file/count limits fail visibly instead of truncating state. No automatic destructive cleanup runs.

**Validation:** VS Code Copilot reports Godot 4.7.2 Mono import and .NET build successful (0 warnings/errors), P4 separate-process write/read 31/33 checks passed, and P1/P2/P3/scenery regression suites passed on commit `ce6767d1b37b34210b010b743ea2133d95c2ae15`. Full commands and warnings are in [P4_Copilot_Handoff.md](P4_Copilot_Handoff.md). **The user subsequently accepted the earlier P4 save/restore/profile functional checks.** Large-save profiling and newer SO2/C1 UI/rendering regression acceptance remain separate; headless tests do not establish FPS.

## SO2 integration — automated validation passed; visual/performance pending
Player/environment/entity references now come from the world's typed ownership API. A narrowly gated source-layout compatibility bridge permits existing SO1/P4 campaigns to Continue without reseeding or losing edits; the next checkpoint uses the verified SO2 fingerprint. Unsupported content/schema still rejects. The user confirmed prior P4 functional tests passed; these SO2 changes require new local tests. See SCENES/World/SO2_Copilot_Handoff.md for the exact internal-test and save-compatibility handoff.

SO2 revision: Godot 4.7.2 build/import, scene ownership and **real SO1-layout checkpoint restoration passed**, including **39/33** P4 write/read. Newer [C1 validation](../COMBAT/C1_Validation.md) extended save checks to **43/39** for schema-2 HP and legacy compatibility. Original P4 [Copilot handoff](P4_Copilot_Handoff.md) covered **31/33** on `ce6767d1`. These are different tested revisions; C1 is the **latest save-test record**. Earlier P4 user functional acceptance and pending SO2/C1 visual/performance checks remain distinct. See [SO2 evidence](../SCENES/World/SO2_Validation.md).

## C1 health checkpoint compatibility

New snapshots use schema 2 with required player HP (zero is a retained dead player). Schema-1 checkpoints lacked this field and restore configured full HP; the next explicit/automatic checkpoint writes schema 2 with the ordinary previous-checkpoint backup. Entity HP/death, frozen geography, edit blobs and content fingerprints remain unchanged. Invalid HP rejects, including values above the runtime actor maximum; there is no silent clamping. Runtime stat modifiers are not persisted before equipment/buff ownership exists. See COMBAT/C1_Validation.md.

SE2 adds narrowly gated compatibility for the shipped seasonal visual resource revisions, composed with existing terrain bridges. It preserves the current schema/fingerprint and does not persist F9 preview state. Source-level historical reconstruction passed; native/disk restoration is awaiting local validation. [Details](../ENVIRONMENT/Seasons/SE2_Validation.md).

H4 writes source completion, chosen final drop outcomes and carried inventory in one detached snapshot. Mid-fall/in-flight saves restore drops settled at their final landing, without replaying the fall or reward roll. Default-valued new chunk fields are omitted when serializing, preserving older immutable blob identities. Schema 1/2 loads grant the starter inventory once. The exact pre-H4 source-content fingerprint bridge is hash guarded; arbitrary changed content still rejects. See [H4](../SYSTEMS/Harvesting/H4_World_Validation.md).
