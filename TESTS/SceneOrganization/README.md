# Scene ownership tests — SO2 awaiting local execution

After building/importing with Godot .NET, run:

```text
godot --headless --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
```

Expected: `SO2 OWNERSHIP PASS: ... checks`, exit 0 and no missing-node/unhandled errors. The fixture instantiates real production worlds in streaming and compatibility modes and checks ownership, shared sorting/identity-container invariants, adapter source counts, player snow/shadow wiring and clean teardown. This is structural runtime validation, not proof of rendered Y sorting or FPS.

See [../../SCENES/World/SO2_Copilot_Handoff.md](../../SCENES/World/SO2_Copilot_Handoff.md) for all regression/save commands and manual requirements. Tests were written but not run by Codex, per user instruction.
