# ENTITIES

Shared actors and entity-specific content will live here. The **player is intentionally located at `ENTITIES/Player/`** for now, matching the chosen layout.

**Status:** Folder and blank configuration template only. No entity base class, movement, combat, or AI is implemented.

- `EntityMasterConfig.cs`: empty Inspector-ready configuration resource template.
- `DefaultEntityConfig.tres`: blank instance for future shared entity settings.
- `Player/`: future playable-character content and settings.

As the project grows, creature families and NPCs can have their own self-contained folders (including their artwork, animations, and sounds). Share behavior in clearly owned components rather than copying it into every actor.
