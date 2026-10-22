# Player / Combat

Future player-facing combat actions, attack state, weapon use and ability requests.

Shared damage resolution, projectiles, status effects, and targetable health should **not** be duplicated for the player; those belong to reusable combat/entity systems when introduced.

**Status:** Documentation/folder only.

**Implementation roadmap:** [COMBAT/COMBAT_IMPLEMENTATION.md](../../../COMBAT/COMBAT_IMPLEMENTATION.md) defines C0–C5 shared player/enemy combat, hit detection, health/stats, feedback and movement. Player input is the request source, not a duplicate damage engine.
