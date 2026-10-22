# Profiles (M0 implemented)

`ProfileStore.cs` stores **only local profile identity** (GUID + display name) at `user://profiles.cfg` using `ConfigFile`. Boot can list, select, and create a profile. This does **not** implement P4 saves, Continue, characters, inventories, world data or permadeath; these remain planned. Do not equate profile creation with world creation. Profile changes do not delete saves.
