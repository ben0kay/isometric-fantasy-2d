# Save serialization — P4 awaiting local validation

`CheckpointFiles` stores SHA256-checked JSON envelopes. Immutable macro/chunk blobs are flushed/published before an atomic checkpoint index replacement. Previous committed index/pointer files are retained as `.bak`; uncommitted `.tmp` files are ignored. Complete referenced transactions are validated on load; failure falls back to a committed backup with a visible recovery notice. Invalid primaries are quarantined before subsequent writes. IDs and blob hashes are canonical/validated before composing paths; display names never become paths.

These guarantees cover individual atomic replacements and process interruption; no universal hardware/power-loss durability claim is made. Shared corrupt blobs may make both checkpoints unrecoverable, which must produce a clear error. See [../README.md](../README.md) and the Copilot handoff for tests not yet run.
