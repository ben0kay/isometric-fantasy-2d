# Shared lightweight particles — C3.3a+b

`ParticleEmitter` is a plain C# helper, not a Node or physics driver. It owns a lazy, reusable slot array (1–256 capacity), bounded emission, motion and drawing. `ParticleProfile` is Inspector-authorable data; `Snapshot()` clamps invalid/excessive settings into immutable values once per burst. `ParticleShapes` shares leaf/shard geometry and simple droplet/spark primitives. [C3.3c/d material profiles and confirmed hit wiring](../HitFeedback/README.md) now consume this core.

## Ownership and use

A consumer supplies its own Node2D canvas, seeded RNG, spawn rectangle/transform, direction and colour. Call `EmitBurst(profile.Snapshot(), count, region, transform, direction, colour, rng)`, wake the owner's existing render callback, call `Advance(delta, wind)` only while active, and call `Draw(canvas)` from its `_Draw`. Queue one redraw when particles change, including the frame they expire or are cleared; sleep when neither particles nor another owned effect needs updates. The engine has no callbacks, timers, scene searches, weather/tree/combat dependencies or stored canvas reference.

Positions, velocities, gravity and injected wind use the **consumer canvas's coordinate space**. The supplied transform affects spawn positions only. A tree uses its existing HitResponse canvas and cached original sprite transform; root scale/mirroring/Y sorting remain inherited exactly as before. Particles never inherit the live sprite's wobble. Callers wanting world-independent particles must choose the appropriate canvas/coordinate conversion explicitly.

Profiles control velocity ranges, directional bias/spread, acceleration, wind, flutter, lifetime/fade, shape, size, spin and stretch. Zero directional spread preserves legacy RNG consumption. Colour is supplied per burst so many species can share a profile; every slot snapshots its colour and motion. Editing a shared Resource affects future bursts only. No per-species hit-colour Resources or default profile files were added. Raw invalid/default ParticleValues cannot create immortal slots.

## Tree migration

TreeHitResponse retains hit registration/count, trunk wobble and VisualTransformChanged. It lazily owns one 24-slot emitter, preserving round-robin replacement, 10 default leaves, original canopy spawn region (20–80% width, 20–58% height), original leaf polygon, 18–29 downward speed, ±12 horizontal speed plus hit X×5, wind×10, flutter, phase/spin/stretch and 2.4–3.8-second lifetime / 0.7-second fade.

TreeDefinition still owns FoliageColour and HasFoliage. TreeHitConfig retains LeavesPerHit/wobble and adds an optional shared/embedded LeafParticles profile override. Burning clears existing healthy leaves and suppresses new ones; no-foliage reset stays foliage-free. Ambient falling leaves are untouched. Wobble restores the trunk after its own window (default 0.65s); leaf-only updates no longer rewrite the sprite or notify shadow renderers. Disable/suspension/unload clears transient particles and restores art; reactivation starts clean.

## Performance and limits

Never-hit/zero-leaf trees allocate no emitter/slot array. First eligible hit allocates once, later hits reuse slots. No per-particle Nodes, rigid bodies, timers, queries, effects manager, actor scans or per-hit child creation. Counts cap at capacity and round-robin replace slots rather than growing. Only active leaves are advanced/drawn, with at most capacity slots examined. Wind is read once per active tree frame. Wobble/shadow updates stop independently. Final clear redraw removes stale canvas commands; settled trees stop processing. Target-PC frame time, GPU cost and allocations have not been measured.

[Validation and local commands](../../COMBAT/C3_3ab_Validation.md). This is a leaf-preserving foundation, not permission to migrate ambient particles or implement combat material effects.

C3.3c/d adds optional `Draw(canvas, worldSpace:true)`: slots retain world coordinates and inverse draw transforms cancel actor translation/rotation/scale while preserving canvas ownership. Default local-space tree drawing is unchanged.
