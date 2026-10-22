# P4 campaign tests — written, not yet executed

`ValidateCampaignSaves.tscn` runs two phases in separate Godot .NET processes:

```text
godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Expected: `P4 PASS: ... checks` and exit 0 from each. The write phase creates isolated fixed-seed A/B worlds, saves different edits/clock/test-actor states, traverses out/back, and exercises corruption/recovery/schema/ownership validation. The read phase must be launched afterward in a new process and checks A→B→A restore. Test state stays in `user://p4-validation` using dedicated test-only IDs and a storage override. Existing real profile files and campaign saves are not touched; old test runs/recovery directories are retained rather than deleted automatically.

No tests/builds were run by Codex, per the requested workflow. See [../../SAVE/P4_Copilot_Handoff.md](../../SAVE/P4_Copilot_Handoff.md) for prerequisites, exact PowerShell commands, regressions, manual checks and reporting requirements.

SO2 follow-up: fixture paths use the explicit actor roots. Write now also restores a legacy-layout fingerprint checkpoint and verifies retained WorldId/map/removal plus the current next-snapshot fingerprint. For an actual previous-revision disk checkpoint, follow the optional read-before-write procedure in SCENES/World/SO2_Copilot_Handoff.md. These added checks have not run here.
