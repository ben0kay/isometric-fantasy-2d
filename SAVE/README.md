# SAVE

> **Status:** Planning/folder foundation only. This pass does not implement systems or content.

Overall save-game lifecycle, versioning, serialization and profile coordination. The world chunk's own change tracking remains in WORLD/Runtime/Persistence; SAVE owns the combined game save and restoration workflow.

## Folder map

- [Profiles/](Profiles/README.md): Future save slots, characters, run metadata and permadeath policies.
- [WorldState/](WorldState/README.md): Coordination of saving/restoring generated world seed, player changes, settlements and persisted chunk deltas.
- [Serialization/](Serialization/README.md): Future save schema, serializers, validation, safe file-writing and atomic backup/recovery policy..
- [Migrations/](Migrations/README.md): Future save-version compatibility and schema migration.

## Conventions

- `SaveMasterConfig.cs` and `DefaultSaveConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
