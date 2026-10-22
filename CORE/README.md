# CORE

> **Status:** Planning/folder foundation only. This pass does not implement systems or content.

Cross-cutting, engine-agnostic contracts and shared infrastructure: stable identifiers, catalogs, events and scheduling. Keep domain logic in its owning module, not as an indiscriminate dumping ground.

## Folder map

- [Identifiers/](Identifiers/README.md): Future stable IDs for content, biomes, equipment, recipes, entities and save references.
- [Definitions/](Definitions/README.md): Reusable base data contracts and serialization-friendly types only where genuinely shared across domains.
- [Events/](Events/README.md): Future decoupled cross-system notifications (e.g.
- [Registries/](Registries/README.md): Future lookup of content by stable ID, including catalog validation and Godot resource resolution.
- [Simulation/](Simulation/README.md): Future centralized simulation scheduling for slow systems (survival needs, crops, spoilage, weather), distinct from frame-by-frame player movement.

## Conventions

- `CoreMasterConfig.cs` and `DefaultCoreConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.

Implemented exception to the planning-only foundation above: [Diagnostics/](Diagnostics/README.md) provides optional, Godot-aware runtime callback timing. It has no gameplay responsibility and is disabled unless a debug capture is requested.
