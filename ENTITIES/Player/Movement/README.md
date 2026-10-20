# Player / Movement

Future player locomotion and physics: movement speed, acceleration/deceleration, collision, sprint, jumps, dodge and other movement modes.

- `PlayerMovement` should own the final movement/velocity application to avoid independent helpers fighting over velocity.
- Sprint, jump, and dodge can later be small dedicated components if they become complex; avoid making empty classes prematurely.
- PlayerInput provides requested actions; Movement resolves gameplay and physics constraints.
- Shared locomotion abstractions can be moved out when NPCs or creatures actually need them.

**Status:** Documentation/folder only; no movement code yet.
