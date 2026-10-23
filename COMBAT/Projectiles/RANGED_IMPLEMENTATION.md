# R0–R2 — Shared Projectiles and Skeleton Archer (Codex roadmap)

> **Status: R0 IMPLEMENTED (23 October 2026); R1 IMPLEMENTED; R2 PLANNED.** See [R0 validation](R0_Validation.md). Implement in requested slices, one job/commit at a time. **R** means Ranged Combat; independent from C3 combat feedback and ES entity separation. Current code has shared C2/C2.5 melee and C3.2 reactions, and the R0 shared straight-projectile runtime. R1 adds the shared archer variant, World service injection and a disposable practice scene. See [R1 validation](R1_Validation.md).

## Desired first encounter

A basic **Skeleton Archer** uses the existing shared `ENTITIES/Shared/Entity.tscn`, carries a visible simple bow, approaches until it has a viable shot, visibly winds up, locks aim just before release, shoots a flying arrow at the player and waits for cooldown. The arrow collides with player or world solids, deals canonical damage if eligible, and expires. Arrows must be dodgeable, not instantly teleported into the player.

**Do not require completion of ES2 or ES4 to begin.** Existing AC actor blocking and ES3 engagement positioning are available; inspect current implementations. Basic ranged position selection must be compatible with the existing movement owner rather than a second AI physics mover.

## Proposed folders and owned responsibilities

```text
COMBAT/
└── Projectiles/
    ├── README.md                       # shared design + future capability catalogue
    ├── RANGED_IMPLEMENTATION.md        # this roadmap
    ├── Projectile.cs                   # R0 transient movement/collision
    ├── ProjectileProfile.cs            # R0 projectile Resource settings
    └── ProjectileSpawner.cs            # R0 only if required for ownership
ENTITIES/
├── Shared/
│   └── AI/
│       ├── GroundChase.cs              # existing single movement owner
│       ├── RangedCombatAI.cs           # R1 generic ranged decisions
│       └── RangedCombatProfile.cs      # R1 range, aim, windup, cooldown
└── Species/Undead/Skeletons/
    ├── Basic/                          # existing melee variant unchanged
    └── Archer/
        ├── README.md
        ├── SkeletonArcher.tres          # shared EntityDefinition variant
        ├── ArcherRangedProfile.tres     # optional shared RangedCombatProfile
        └── Sprites/                    # placeholder archer sprite if needed
ENTITIES/Species/Undead/Skeletons/Archer/
└── Equipment/Bow/
    ├── SkeletonBow.tres                 # enemy presentation definition
    └── Sprites/skeleton_bow.svg         # artwork referenced by the definition
FEEDBACK/                                # optional visual events, not damage authority
```

**Don't create every prospective file upfront.** Start with README/planning; then create only the runtime files and Resources that R0/R1 actually use. Do not invent a separate SkeletonArcher motor/HP implementation. A projectile is not a full `Entity.tscn`, but optional health/targetability must be possible later.

## Phase R0 — Shared projectile fundamentals

1. Read `CodePreferences.md`, current combat interfaces and validation, current AC physics layers/masks and world ownership before implementation.
2. Build minimal reusable projectile runtime and Resource profile for **straight arrows**: speed, visual, lifetime/max travel, collision layers/masks, damage and hit ownership. Snapshot attack/attacker parameters on fire, avoiding recalculation while in flight.
3. Swept segment collision per physics step between previous and next projectile position. Prefer Godot space-state ray/shape sweep suitable for the arrow's authored hit radius, not mere new-position overlap. Respect layer 1 world solids and layer 2 actor bodies, source exclusion, faction/eligible hits, and closest blocking collision. Document interactions with props/trees; **do not trigger wood-chopping hits simply because arrow touched a tree**.
4. On eligible actor impact call the existing canonical damage receiver/resolver exactly once, preserving directional guard and genuine HP-loss events used by damage numbers and feedback. Decide what happens on ineligible actor/solid collisions (default: stop on solid obstacle). No friendly-fire surprises, phantom repeated hits or post-despawn impact callbacks.
5. Give the projectile lifetime/loaded-world bounds; unload/scene exit cancels cleanly. Avoid forcing chunk generation or persisting temporary projectiles to P4 saves. Cap projectile count, avoid per-shot expensive Managers and uncontrolled allocations; add a spawner/helper only if it meaningfully simplifies correct ownership.
6. **Explicit future extension seams:** movement mode (straight now, homing/arcing later), on-impact policy (stop now, pierce/chain/area later), optional `ActorHealth`/combat target on special projectiles, lifecycle event for later VFX/pickups. No large inheritance forest, no dummy components for future behavior.
7. R0 acceptance: fixture fires arrow at player from multiple distances/directions; ray cannot tunnel through thin target at high speed, stops on a tree/wall, avoids source and friendly targets per chosen policy, honors frontal/rear guard mitigation, only one damage application, cleans up on scene unload and when max life/range reached. Null/stale owner and zero/invalid profile data are safely rejected.
8. Run relevant C1/C2/C2.5/C3.2 tests, Godot .NET build/headless import, P3/SO2 lifecycle and P4 save compatibility as appropriate. Record observed results; don't relabel existing headless shutdown issues as successes.

## Phase R1 — Data-driven Skeleton Archer and shared ranged AI

1. Use one **shared ranged combat AI** behavior with optional `RangedCombatProfile` for range minimum/maximum, preferred distance, aim lock time, windup, cooldown, firing line of sight, projectile profile and spawn offset. Avoid special-case skeleton-only ranged controllers.
2. Reuse the existing EntityDefinition, ActorHealth, faction/stats, damage numbers, resistances, AC collision, C3.2 stagger/knockback and GroundChase movement owner. Expose ranged behavior opt-in without silently enabling existing melee profile. Ensure ranged and melee intents cannot concurrently own the same attack state.
3. AI approaches until inside firing distance, checks line of sight against scenery/actors, halts or slows to visibly aim, **locks fire direction shortly before launch**, fires straight shot and respects cooldown. Prefer clear readable shots over perfect homing/prediction.
4. Ranged skeleton variant goes under `ENTITIES/Species/Undead/Skeletons/Archer/`. Use temporary readable bow and arrow visuals; enemy bow data/art lives in `Archer/Equipment/Bow/`, as requested. Player inventory weapons remain separate under `ITEMS/`. Don't copy AI into the bow Resource. Keep existing Basic skeleton's data and F6 five-melee setup intact unless an explicitly scoped practice addition is requested.
5. Provide opt-in range positioning, not perpetual running away. ES3 position reservations can be reused where appropriate but **ranged firing positions need not equal melee engagement slots**. Respect obstacles and loaded chunks; avoid constantly requesting full A* paths.
6. R1 acceptance: archer detects player, finds valid firing position, visibly aims, fires on cooldown, arrow can miss when player moves, an obstacle blocks shots, death/stagger cancels wind-up when interruption policy permits, owner unspawn/disable clears pending shots/intent, and multiple archers don't lock each other indefinitely.

## Phase R2 — Visual feel, practice and test handoff

- Improve bow pose/aim/fire cues, arrow flight and impact feedback (using existing FEEDBACK particles/material profiles where available); do not require finished sprite sheets.
- Add **optional** mixed melee/archer practice setup (preserve current five-skeleton F6 test or make selection configurable) and the compact ranged controls in the existing F9 Combat section, following its existing container/toggle conventions. Avoid a second HUD and don't clutter F9 by default.
- Tune speed, fire interval, aim lock, maximum range, spawn offset and visual arrow readability. Keep tuning data-driven, with optional debug overrides isolated from save data.
- Measure 1, 5, 20+ active projectiles; verify no tunnelling, duplicate damage, out-of-bounds shots, attack starvation, excessive allocations, visual z-order or projectile lingering through scene transitions.
- Validate Godot build/import, focused projectile/archer fixtures, C1/C2/C2.5/C3, actor collision ES, P3 streaming, SO2 ownership and P4 save/load; report exact observed outcomes and unresolved baseline crashes separately.
- Record R0/R1/R2 status, changed files and local/manual visual acceptance. Do not declare completion solely from compilation.

## Future-facing capabilities — not part of initial skeleton archer

See [the projectile capability catalogue](README.md#future-rpg-inspired-feature-catalogue-design-extension-points-not-r0-requirements): piercing, ricochet, fork, chaining, return, homing, optional projectile health, reflects, explosions, status effects, fields, embedded arrows/recovery, arcing, multishot and environmental impacts.

A **destructible mystical orb** may opt into real health and combat targeting, but normal arrows must remain small transient objects. Never build an always-ticking full actor AI/health stack into every basic projectile merely for hypothetical future features.

## Suggested Codex requests

**First job:** “Implement **R0 only** per `COMBAT/Projectiles/RANGED_IMPLEMENTATION.md`, focusing on one correct low-overhead straight arrow, existing shared damage/guard contract, swept collisions and streamed lifetime. Keep future optional health/homing extensible but do not implement them yet. Validate and document.”

**Second job:** “Implement **R1 Skeleton Archer** with a shared ranged AI and data-driven ranged profile, using R0 projectiles and existing `Entity.tscn`; preserve melee skeleton behaviour and collision/ES/knockback. Validate and document.”

**Third job:** “Implement **R2** visual/tuning/practice HUD and regression checks; do not broaden gameplay without approval.”

## R1 ownership decision and current entry point

The user requested the enemy bow remain with this Archer variant, separate from player items: `Archer/Equipment/Bow/SkeletonBow.tres` references `Archer/Equipment/Bow/Sprites/skeleton_bow.svg`. Shared presentation code is under `ENTITIES/Shared/Equipment/`. No player bow definition/art is created by R1.

Open `TESTS/Projectiles/ArcherPractice.tscn` and press F6 to play one archer with real player combat and cover. Default World still spawns its five Basic melee skeletons. A future/explicit ranged EntityDefinition in World placement receives the lazy scene-local projectile service. R2 mixed encounters/F9 ranged controls remain planned.
