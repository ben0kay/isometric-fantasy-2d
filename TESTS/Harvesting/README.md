# H0–H3 harvesting practice

Open `HarvestingPractice.tscn` and press F6. Dedicated disposable room; no campaign disk writes or production world generation.

- Arrows move; 1 axe, 2 sword. LMB gives one valid axe chop; fourth chop fells a tree. MMB prods and sword attacks add no work.
- Oak starts to your right. Mirrored Blossom below-left uses the identical shared tree/profile. Generic work target below-right demonstrates extraction without tree behavior.
- Logs scatter/hop/tumble, then settle with grounded shadows. Move within 60 units and press E for each nearest pickup. Full inventory leaves drops; I/Esc opens/closes the real backpack.
- Manual Spawn 3 test logs button exercises H1 independently of harvesting. Blue rectangle is a landing-exclusion test area, not production water.
- All counts, source progress and drops reset when the fixture restarts. Normal World/campaigns are now enabled by H4; this fixture remains disposable. No stump/regrowth/falling collision/damage.

## Tuning

Select the root's TreeHarvest embedded Resource: RequiredWork=4, AcceptedTool=axe, DropTable log quantity 3–5. DefaultDropScatter.tres owns hop/spread/rotation/radius/budgets. TreeDefinition.FallSeconds defaults to 0.65; production tree Resources are never edited by the fixture. The fixture uses runtime copies for axe work and optional tree profiles.

`ValidateHarvesting.tscn` verifies authoring snapshots, independent deterministic rolls, progress/retry, real confirmed axe hits, full/mismatched packs, reentrancy, bounds/water predicate/solid landings, pickup wall/range/death/airborne checks, actual E/I routing, fire precedence, mirrored anchor/fall direction and interrupted proxy release. [Exact commands, results and remaining acceptance](../../SYSTEMS/Harvesting/H0_H3_Validation.md).

`ValidateWorldHarvesting.tscn` exercises actual World, streaming and disk checkpoints: run `-- --h4-write`, then `-- --h4-read` in a separate process using the same user-data directory. Its dedicated `user://h4-validation` namespace never uses real campaign saves. Optional `--legacy-root=/absolute/old/p4-validation` reads untouched prior-revision P4 fixtures and upgrades a copy.
