# Player combat actions

C2.5 `PlayerActions` is the single small action gate shared by the existing equipment adapter and PlayerMovement: Idle / Attack / Block / Dodge / Hop. It is plain state, not a new Node or mover. Actions are not queued. An attack stays committed through its swing/recovery; blocking/hopping/dodging cannot cancel it. Its extra cooldown still prevents another attack or weapon switch. Guard/hop may begin once the swing has finished; no waiting press is replayed automatically.

A successful dodge cancels blocking, then excludes attack/block/hop during its active duration. A held RMB does not automatically re-enter guard after that cancellation: release and press again. Blocking overrides sprint with the configured slower speed; releasing restores normal held sprint. Death/suspension discards transient state.

PlayerMovement owns authoritative movement/HP/shared guard and the visual hop. EquippedTool owns main-hand selection, shield-equipped flag, intent routing and one C2 attack executor. Input owns all mappings. No second HP, damage engine, inventory or physics mover. [C2.5 validation](../../../COMBAT/C2_5_Validation.md).
