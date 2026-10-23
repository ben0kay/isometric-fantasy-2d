# COMBAT / Damage

`DamageResolver` is pure shared arithmetic plus one explicit HP mutation. `ICombatant` exposes health/stats and a common receiving entry point for player/Entity. `DamageRequest` carries base damage; `DamageResult` records requested/resolved/applied damage, before/after HP and whether this hit killed.

Resolve: base damage × attacker damage multiplier − target flat defense, clamp to zero, round once away from zero, cap at 1,000,000. There is no automatic one-damage minimum. Applied damage caps at remaining HP; dead targets cannot die again. Zero/blocked hits emit no health change or flash. Negative/nonfinite requests and invalid external effective stats reject.

No collision queries, targeting, attack executor, scene paths or feedback dependencies. C2 now supplies snapshotted attacker stats through shared melee execution; the legacy `Entity.ReceiveHit` still supplies base damage 1 with neutral attacker stats, preserving default tree/axe behavior. Categories, resistances and armor systems remain future work.
