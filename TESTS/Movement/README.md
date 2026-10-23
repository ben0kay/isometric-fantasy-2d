# Movement regression fixtures

`ValidateSeparation.tscn` runs64 ES1 checks against shared actors, real broad-phase membership, the existing GroundChase motor, reactions and narrow-lane crowds of1/2/5/10. No production species scripts or settings are changed on disk.

`SeparationWorldProbe.gd` compares a disposable fixed-seed five-skeleton World over600 physics frames. Pass `-- --separation-off` for the baseline; omit for enabled. It keeps the player alive/stationary through in-memory settings only.

See [ES1 results and manual checks](../../ENTITIES/Shared/Movement/ES1_Validation.md).
