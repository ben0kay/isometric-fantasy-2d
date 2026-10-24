# Gameplay UI integration checks

Open `ValidateGameplayUI.tscn` and run F6, or use the headless command in [UI1/UI2 validation](../../UI/HUD/Gameplay/UI1_UI2_Validation.md). Uses real Player/Input/HUD scenes and injected viewport input without campaign disk writes. Checks nine slots, I/Esc/focus, command suppression, held movement/guard/sprint, close button, viewport sizing, teardown and repeated composition. The campaign Esc stage is represented by an unhandled-key observer; real campaign Continue/menu and visuals still need local acceptance.
