# Scene ownership tests — SO2 automated validation passed

After building/importing with Godot .NET, run:

```text
godot --headless --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
```

Expected: `SO2 OWNERSHIP PASS: ... checks`, exit 0 and no missing-node/unhandled errors. The fixture instantiates real production worlds in streaming and compatibility modes and checks ownership, shared sorting/identity-container invariants, adapter source counts, player snow/shadow wiring and clean teardown. This is structural runtime validation, not proof of rendered Y sorting or FPS.

See [../../SCENES/World/SO2_Copilot_Handoff.md](../../SCENES/World/SO2_Copilot_Handoff.md) for all regression/save commands and manual requirements. Codex subsequently ran this fixture: 60 checks passed, exit 0, with Godot 4.7.2 .NET. All listed regressions and legacy-save proof also passed; see SCENES/World/SO2_Validation.md for evidence and remaining manual acceptance.
