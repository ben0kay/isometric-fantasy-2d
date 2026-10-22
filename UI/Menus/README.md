# Menus — P4 integration implemented and automated-tested; manual acceptance pending

F5: BootScreen → existing local GUID/name ProfileStore → MainMenu. New Campaign now passes independent profile/run/world/seed identity to production World, whose first successful checkpoint activates the campaign. If a campaign exists, replacement requires explicit confirmation; prior run files remain archived. Continue is enabled only after the selected profile's durable transaction and current content are verified. Invalid saves show an error; recovered backups show a notice. Menu IO runs off-thread with selection locked, and menu routing remains independent of World child paths.

Campaign F8/Esc/save-before-close UI is owned by SAVE's coordinator under one CanvasLayer. Options, multiple-slot/archive browser, inventory and permadeath deletion remain unimplemented. Shared main-menu buttons still use UI/Shared/Buttons/UIButtonFactory.cs. F6 direct World remains disposable; existing DEBUG prototypes are preserved.

See SAVE/README.md and SAVE/P4_Copilot_Handoff.md for policies, recorded automated passes and outstanding real in-editor save/quit/profile UI tests.
