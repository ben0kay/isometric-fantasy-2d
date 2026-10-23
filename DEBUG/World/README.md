# World practice actors

`DebugActorRoot.cs` supplies the Inspector **Enabled** switch on World.tscn → WorldActorsRoot → PersistentEntitiesRoot → **DebugActorsRoot**. Turn it off before F6 to spawn no test skeletons. When switched off in the Remote Inspector, existing skeletons hide, stop AI and leave physics; switching on resumes surviving spawned skeletons. If disabled at startup, reopen World to populate after enabling it. Visibility alone is not the disable switch.

The root contains four editable Marker2D offsets relative to the initial player spawn, not absolute map coordinates. F6 places the original skeleton plus four more at nearby walkable, separated positions, using canonical Entity.tscn/SkeletonBasic.tres and one shared navigation grid. A bounded startup search adjusts markers around scenery; logs warn if there is no safe position. They do not respawn after death in the same session.

Campaign New/Continue keeps only the original saved test skeleton; four additional practice enemies are F6-only and are never written to campaign snapshots. The root toggle includes the original skeleton. This layout changes the scene-content fingerprint; previous campaigns are preserved but may refuse Continue. No schema bypass/migration was added.

No ticking debug controller or crowd-avoidance system is introduced. Enabled skeletons use ordinary GroundChase; AC3 crowd/performance acceptance remains open.
