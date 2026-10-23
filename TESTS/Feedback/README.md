# Feedback tests

`ValidateParticles.tscn` tests C3.3a+b's real particle helper and production tree adapter. Checks cover legacy spawn/trajectory/wind parity with seeded RNG, overflow/lifetime/reuse, snapshot colour/lifetime, invalid/default settings, oak/blossom plus mirrored/scaled synthetic art, unchanged child counts, independent wobble/shadow notification shutdown, burnt/no-foliage eligibility, disable/suspension/reactivation and settled idle.

Run `godot --headless --max-fps 60 --path . res://TESTS/Feedback/ValidateParticles.tscn` after building/importing with the project's Godot Mono/.NET versions. See [validation](../../COMBAT/C3_3ab_Validation.md); headless correctness does not establish rendered equivalence or target-PC performance.

`ValidateHitFeedback.tscn` checks shared material resources, authoritative contact provenance/closest point, positive/zero/armour/blocked/chip/lethal hits, misses, generic damage/restoration exclusion, actor-to-player flesh, one burst per swing, lazy allocation/reuse/caps, expiry and suspension/unload. [C3.3c+d validation](../../COMBAT/C3_3cd_Validation.md).
