# C2.5 — Player actions, dual attacks and starter equipment (Codex handoff)

> **Status: IMPLEMENTED; headless validation and remaining checks recorded in [C2_5_Validation.md](C2_5_Validation.md).** Target-PC visual/feel acceptance and a final normal SDK build remain separate. The design brief below is retained as the requested scope, not further work to implement automatically.
> **Intent:** Keep the production player responsive with one script owning controls, shared combat execution, swappable starter axe/sword, optional shield blocking and an RMB jump when no shield is equipped. No inventory or authored sprite animations required.

## Read before touching production

- Root `README.md` + `CodePreferences.md`; `COMBAT/COMBAT_IMPLEMENTATION.md`; `COMBAT/C2_Validation.md`; relevant player/equipment/item README files.
- `ENTITIES/Player/Input/PlayerInput.cs`: existing LMB event, arrow movement, RShift sprint/double-tap direction dodge; preserve all input.
- `ENTITIES/Player/Movement/PlayerMovement.cs`, `PlayerMasterConfig.cs`: one CharacterBody2D motor, P3 streaming guard, health/stats, dodge precedence. **Avoid a second motor.**
- `ENTITIES/Player/Equipment/EquippedTool.cs`: single-tool player adapter, cursor facing, radial full-turn attack, reusable C2 `AttackExecution`. Refactor carefully; don't add per-weapon copies of this logic.
- `COMBAT/Weapons/{AttackExecution.cs,AttackProfile.cs}`, `COMBAT/HitDetection/MeleeHitQuery.cs`, `COMBAT/Targeting/`: timing/targeting/damage shared with enemies; use unchanged where possible.
- `ITEMS/Tools/Axes/{Axe.tscn,AxeConfig.cs,DefaultAxeConfig.tres}`: active axe artwork and existing tree/prop hit behavior.
- `ENTITIES/Player/Player.tscn`, `ENTITIES/Shared/EntityDefinition.cs`, `FEEDBACK/HealthBars/`, `SAVE/`, `SCENES/World/`: preserve SO2/P4 and C1 components. Verify whether there is an existing sword/shield asset before creating art or references.

## Controls and equipment rules

| Control | Behavior |
| --- | --- |
| LMB | Equipped main-hand weapon's **primary** attack |
| MMB | Equipped main-hand weapon's **alternate** attack |
| RMB, shield **not equipped** | Jump (one tap triggers a short visual hop; no combat invulnerability by default) |
| Hold RMB, shield **equipped** | Directional shield block, aimed toward cursor; no separate parry yet |
| Arrow keys | Existing movement; retain current shift sprint and double-tap dodge |
| Starter switching | Provide a **small temporary direct selection/toggle** to try axe vs sword and shield-equipped vs unequipped, without building inventory. Choose nonconflicting keys, document them in PlayerInput README and avoid UI/menu shortcuts; keep all mappings centralized in PlayerInput. |

Player starts with **axe, sword and shield available for immediate testing**, but only **one main-hand weapon selected at a time**. Shield occupancy is a separate boolean/equipment flag; it must be possible to test RMB jump and RMB block in one session. Suggested startup: axe selected, shield **unequipped** (so jumping is immediately testable); sword and shield can be equipped with temporary test keybindings. Do not spawn three actively swinging weapons at once. Codex may propose simpler debug switching if conflicts discovered.

### Data contract and scene ownership

- Model generic `PrimaryAttack` and `SecondaryAttack` as references to **two independent reusable `AttackProfile` Resources** on equipped item definitions (or an equivalent small generic weapon profile). Never hardcode numerical melee damage inside `PlayerInput` or clone the entire C2 pipeline.
- Axe gets a reusable **sweep** presentation for primary and **linear forward prod/thrust** presentation for alternate. Both are C2 attacks with separate contact geometry and timings. Pose/animation must match the true contact direction and contact window; no false impact when physics reports a miss.
- Separate *weapon visual animation* from damage/hit detection. Sprite pivots/offsets use existing axe artwork; do not rotate the entire player collider or invent character animation assets.
- Generalize `EquippedTool` into a shared player equipment adapter when justified, or minimally extend existing adapter without an axe-only state machine that cannot support a sword. Sword can use a **clearly labelled temporary existing/simple sprite placeholder** if no sword texture exists; do not claim actual art was authored. Keep asset identity/data under ITEMS (weapons/tools) and rendering under player equipment, not in COMBAT's damage code.
- Blocking belongs to `COMBAT/Defense/` as a small reusable defensive contract applied on incoming C1 damage requests; player-specific RMB routing under Player Input/Combat. Shield **equipped** enables sustained facing-limited block, with configurable mitigation (e.g. starter 60% reduction) and cone; no shield means no block. A future enemy can adopt the same contract. Existing armor/flat defense must remain functional; document resolution order and make it deterministic. Blocking cannot make HP negative or create an independent HP pool.
- For shield display, because no shield sprite exists: **do not block implementation on art**; use an optional simple debug geometry/guard marker or no shield visual, gated by inspector and not exported as a finished asset. Do not introduce a shield inventory system.
- Jump is an early simple visual hop: affect *visual offset* of player artwork only, preserve feet collider and actual world coordinate (including SO2 Y-sort position), no jumping over obstacles, chunk boundaries or entities, no i-frames. Configurable height/duration, no jump spamming. Respect dodge/attack/block state interactions.
- Centralize mutually exclusive states: while blocking, do not start attack; while attacking, either reject block until the committed swing ends or define a carefully tested cancellation rule. Dodge should have clear, deterministic priority (prefer preventing block/attack during active dodge). No runaway queuing, and releasing RMB ends block immediately.
- Prefer minimal modular code and no idle callbacks for cooldown/guard checks where possible. Existing equipped-tool cursor pose _Process may continue if necessary, but don't add redundant global timers/scans.

## Suggested first-pass balance (all inspector-tunable, **not** current values)

| Property | Axe primary swipe | Axe alternate prod |
| --- | ---: | ---: |
| Base damage | 11 | 5 |
| Reach | 52 px | 60 px |
| Total attack time | 0.38 s | 0.28 s |
| Contact area | Wide forward arc | Narrow frontal arc / short thrust |
| Contact timing | Matches radial swing arc | Near full extension |
| Recovery/cooldown | 0.15 s | 0.45 s |
| Target knockback | Low / future C3: 15 px | Strong / future C3: 55 px |

**Important:** C2 `AttackProfile` has no knockback property or motor integration yet. C2.5 should either add **one simple shared contact knockback impulse interface** (with no new standalone physics mover), or explicitly defer the physical knockback to C3 and document the exact profile field/event contract. Do **not** imply that a visual prod already displaces skeletons. If implementing knockback, maintain Nav/CharacterBody2D collision and streaming restrictions and don't teleport targets through walls.

**Skeleton balance:** existing skeleton `MaxHealth=3` in `ENTITIES/Species/Undead/Skeletons/Basic/SkeletonBasic.tres`; axe defaults to `BaseDamage=1`. To actually feel 11/5 damage, propose/update the authored skeleton to **50 HP** and consider 2 defense (after inspecting current stats data). **P4 WARNING:** changing a saved entity's MaxHealth or authored Resources can reject existing P4 checkpoints via content fingerprints or make restored HP incompatible. Do not silently break existing campaigns; inspect migration/versioning requirements, document the deliberate decision and test old saves. If safe migration cannot be justified, leave production skeleton tuning untouched and use a dedicated disposable combat test definition at 50 HP. Do not change global C1 damage formula to force balance. C1 resolves `max(0, base*attacker multiplier - defender defense)`, then rounds to integer.

**Sword initial behavior:** give it a separate Resource and working primary + alternate profile; favor a straightforward sweep and a quicker narrower thrust, with intentionally basic timing so the switching/test harness proves generalization. Exact values can be adjusted in the Inspector. Keep the axe's prop interaction enabled and ensure sword does not accidentally chop trees unless intended.

## Specific implementation sequence (Codex can refine with a short rationale)

1. Confirm up-to-date C2 integration/scene/assets and save gates. Establish two attack intents + movement/defense intent in PlayerInput; don't move physics there.
2. Introduce minimal equippable main-hand selection + separate shield-equipped flag (temporary test controls). Configure Resources, no inventory/hotbar.
3. Wire primary/alternate requests to shared `AttackExecution`; primary sweep uses existing radial sprite motion; alternate moves along cursor axis out-and-back and remains visually pointed outward, with appropriate narrow contact. Both must obey busy/cooldown and target contact rules.
4. Configure axe at 11/5, add starter sword with functional profiles and a placeholder visual if required. Avoid unnecessary scene child explosion. Determine balance/save-safe skeleton plan.
5. Implement no-shield RMB visual jump in existing movement/presentation ownership and shield-equipped RMB guard through reusable defense rules. Test release/attack overlap/dodge priority, behind-target and damage mitigation. No parry required.
6. Reconcile save tests and world scene fingerprints, update relevant README/plan and produce validation handoff. **One Git commit** for this C2.5 job.

## Required tests and acceptance

- `dotnet build` Godot .NET 8; headless editor import and the C2 production fixture + C1 + P4 write/read and old checkpoint read + SO2 and relevant P3 smoke/regressions. Record exact exit codes; C2 baseline documented some environment shutdown failures—do not mislabel known failures as passes or silently ignore new ones.
- In disposable combat scene and F6 World: LMB visibly **sweeps**, MMB visibly **prods** toward cursor; contact direction/reach/narrowness matches hit behavior; no double damage from overlapping contact samples.
- Axe hits skeleton with 11 damage before modifiers and 5 damage on alternate in a suitable 50-HP test target. Normal defense applies; visual misses don't cause damage. Sword uses same shared execution with own profiles.
- Key switching doesn't leave two attacks processing; cooldown/death/unload/reset unsubscribes and cleans up correctly. Both shield modes testable without inventory.
- RMB with shield absent plays a simple hop without moving the world physics body through obstacles; RMB held with shield up mitigates only eligible *frontal* contacts; behind and unguarded hits remain normal. Shield release stops guard. No invisible invulnerability or block while dead.
- Arrow movement/sprint/dodge/aim, tree axe interactions, enemy attacks, feedback health bars, streaming/SO2 actor ownership and P4 saves continue to work.
- No new per-frame scene-wide scans or extra idle actor processing; any visual animation sleeps when complete. Validate collision guards and performance with multiple actors.
- Update `COMBAT/COMBAT_IMPLEMENTATION.md` status to implemented **only when verified**, and link from appropriate player/equipment READMEs. Add `COMBAT/C2_5_Validation.md` recording actual checks, limitations and manual handoff.

**Out of scope:** inventory, crafting, shield sprite, fully animated body, animation trees, parry/perfect block, stamina or durability, sophisticated jump physics/ledge crossing, shield bash, new enemy species, C3 full hit FX/health UI and complete rebalance of existing save campaigns.

## Agreed choices applied

Forward slash instead of full spin; slower blocking overrides sprint; visual-only hop; successful dodge cancels guard; committed attacks finish before other actions. Keys 1/2/3 select axe/sword/toggle shield; start axe/shield off. Production skeleton remains unchanged; a disposable 50-HP practice target exercises 11/5 balance. Physical knockback waits for C3. Requests rejected during another action are not queued; release/repress RMB after cancellation. See validation for actual checks, not blanket acceptance.
