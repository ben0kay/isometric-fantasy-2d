# AUDIO

> **Status:** Planning/folder foundation only. This pass does not implement systems or content.

Shared music, sound routing, world ambience, reusable audio players and effect definitions. Player-specific footsteps, breathing and character vocalizations may remain under ENTITIES/Player/Audio while using central mixers/playback helpers.

## Folder map

- [Music/](Music/README.md): Music themes, transitions and any future contextual scoring; mood can depend on time, biome and encounters..
- [Ambience/](Ambience/README.md): Biome, location, cave, seasonal and weather ambience profiles and layers.
- [Sounds/](Sounds/README.md): General reusable sound effects such as impacts, inventory feedback, doors, crafting, weather and combat.

## Conventions

- `AudioMasterConfig.cs` and `DefaultAudioConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
