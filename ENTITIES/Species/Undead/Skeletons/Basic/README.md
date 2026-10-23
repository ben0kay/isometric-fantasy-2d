# Basic Skeleton

Resource-only variant: `SkeletonBasic.tres` and `Sprites/skeleton_basic.svg`. Uses `ENTITIES/Shared/Entity.tscn`. The Resource enables optional `ChasePlayer` pursuit with existing speed/range values and has 50 HP, red damage flashes and fade-out on death. Future Avalonia tooling can author the Resource without a scene per enemy variant.

The regular World references this definition directly, so its placed skeleton starts at 50 HP too. This is authored content tuning, with no extra scripts or processing. Existing campaigns may be rejected by the strict content fingerprint check; use a new campaign or disposable F6 World. No automatic migration or save deletion is performed.
