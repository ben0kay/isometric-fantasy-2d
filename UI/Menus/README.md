# Menu foundation — M0 implemented

Startup F5: `Boot/BootScreen.tscn` → local profile selection/creation (`SAVE/Profiles/ProfileStore.cs`) → `Main/MainMenu.tscn` → **New Campaign** opens production `SCENES/World/World.tscn`. Menu routes do not depend on world node paths, so SO0–SO3 may proceed separately. F6 directly runs a selected `World.tscn` for development.

**Not implemented:** P4 world saves/Continue (button intentionally disabled), persistent campaigns, profile-specific world restore, Options and Pause. Starting New Campaign currently creates a fresh session world and does *not* save it. No automatic deletion/replacement of any world files or permadeath policy. The presentation is deliberately fantasy-themed with static lightweight styling; future artwork belongs here.

Shared buttons: `UI/Shared/Buttons/UIButtonFactory.cs` and editable `DefaultButtonStyle.tres`; apply these for inventory/HUD too. Menu scenes are independent `Control` roots. No changes to production World.tscn or DEBUG test scene.
