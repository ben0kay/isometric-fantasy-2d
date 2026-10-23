# Shared player equipment adapter

C2.5 `EquippedTool` renders one held item and submits primary/alternate requests to one shared `AttackExecution`. `EquippedItemDefinition` references two independent attack profiles plus texture/grip/scale/rotation. Axe and sword share the adapter; only temporary starter selection references the two defaults. No per-weapon attack state machines or inventory.

Primary forward slash crosses locked forward aim at its contact time; alternate thrust extends/retracts along that aim and reaches maximum extension at its own contact time. Rendering uses snapshotted presentation values and does not create impact feedback on misses. Timings/arc/reach/damage remain in COMBAT profiles. Existing `AxeConfig`/scene Config remains a compatibility source for Enabled/HandAnchor/OrbitRadius/AimSpeed; actual starter damage/timing now comes from item attack Resources, not its inherited legacy attack fields. The old HitQuery child/API remains for compatibility callers/tests.

Keys 1/2 select axe/sword; 3 toggles a separate shield flag. Shield starts off. Switching rejects during actions or outstanding attack cooldown, so it cannot bypass recovery. RMB routes to guard only when equipped, otherwise to a visual hop. The optional blue guard arc is debug geometry, not shield artwork. Starter selection/flag is temporary, resets to axe/shield-off on scene load and is not inventory or persisted equipment.

Attack physics wakes only for accepted swings/cooldowns and sleeps on completion/cancellation. The existing mouse-facing render callback remains and also positions the held item during a hop. Death, disabled scene ownership and exit cleanly cancel/unsubscribe/dispose. [Validation/handoff](../../../COMBAT/C2_5_Validation.md).
