# ENTITIES

Actors and entity-owned content live here: the player, eventual wildlife, monsters and NPCs. The player remains under [Player/](Player/README.md).

**Status:** Production actors beyond the current debug test have not been implemented. `EntityMasterConfig.cs` / `DefaultEntityConfig.tres` are inspector-ready placeholders. The active disposable top-down test uses its own character and settings under `DEBUG/TopDownTest/`.

- Keep actor-owned sprite variants, state, species behaviour and entity-specific data with their actor/species folder.
- Shared combat resolution will belong in `COMBAT/`, generic item definitions in `ITEMS/`, and shared interactive gameplay mechanics in `SYSTEMS/`.
- Biomes under `WORLD/Biomes/` eventually reference eligible creature species and their spawn conditions; they do not duplicate actor assets or AI.
- Player-specific survival needs and body temperature are planned under `Player/Survival/`, driven by conditions in `ENVIRONMENT/` and `WORLD/`.

Avoid copying generic behavior into each creature or player implementation.
