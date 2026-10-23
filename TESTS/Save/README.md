# P4 campaign tests

`ValidateCampaignSaves.tscn` runs write/read in separate Godot Mono processes:

```text
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Require PASS output, exit 0 and no ERROR lines. Write creates fixed-seed A/B campaigns with different edits, clock, player/test-entity HP/death, traversal, corruption/recovery/schema/ownership cases. Read checks A→B→A restore in a new process. Tests use dedicated IDs and `user://p4-validation`; real profile/campaign storage is not used. Isolate XDG_DATA_HOME for independent Linux runs.

AC4 corrected an obsolete unconditional SO1 layout-bridge assertion. Later scene layouts must reject that old fingerprint; schema-1 HP compatibility is checked independently using compatible current content, followed by schema-2 save. Exact historical SO2 layouts retain their gated bridge check. No production migration policy changed. Test actor lookups use World.TestActorsRoot; campaigns retain the original single-skeleton contract.

See [AC4 current results and limitations](../../ENTITIES/Shared/Movement/AC4_Validation.md), [original P4 handoff](../../SAVE/P4_Copilot_Handoff.md), [SO2 historical migration evidence](../../SCENES/World/SO2_Validation.md), and [C1 historical HP tests](../../COMBAT/C1_Validation.md). Actual previous-revision disk migration is distinct from the synthetic schema-1 fixture and was not repeated in AC4. Headless tests do not establish Windows rendering or FPS.
