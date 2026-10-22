# Shared / Health

`ActorHealth` is the single HP owner used by player and Entity. Positive damage/healing clamp to 0/Maximum and emit `Changed` only when HP changes. Healing cannot revive; explicit validated `Restore` can. The change marks restoration so feedback can skip damage effects. No Node, callback, timer or polling. Maximum is fixed at construction; runtime max-HP modifiers are outside C1.

Damage math and immutable results live in `COMBAT/Damage`; actor adapters own death/feedback behavior. Entity retains its flash/fade/free behavior, while player death stops input, movement and equipment and retains the player/save. A death/retry UI is future work.
