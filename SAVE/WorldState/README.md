# Campaign world state — P4 awaiting local validation

`CampaignSnapshot.cs` defines detached player/clock/test-actor/chunk records. `CampaignSession` carries a one-shot new/continue request into the existing World scene and checks content compatibility. `CampaignStore` validates ownership/schema/integrity and commits durable checkpoints. `CampaignSaveController` captures world/player state on the main thread, sends detached values to one worker, and owns the autosave timer and F8/Esc/close flow. No always-running save frame callback is added.

WORLD owns exact macro restoration and meaningful prop deltas; SAVE coordinates the chosen profile/run/world. No inventory/survival/A-Life gameplay is introduced. See [../README.md](../README.md) for policies, format, limits and testing handoff.
