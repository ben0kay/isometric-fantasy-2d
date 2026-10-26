# H4 — real World harvesting

Implemented 25 October 2026. H5 remains planned. Windows visual/performance acceptance is pending.

## Play in the real world

Run normal F5 → New Campaign/Continue, or F6 `SCENES/World/World.tscn` for a disposable World. Select starter axe with 1; land four LMB chops on Oak or Pink Blossom. The rooted artwork starts falling **at the same time as 3–5 Wooden Logs scatter/hop**, then bounce briefly on ground contact and settle at varied angles. E collects a nearby settled log into actual Player storage; I shows counts. 2 selects sword; 1–9 selects shortcuts. G toggles the temporary shield (no shield item yet). F8 saves a campaign; Esc supports save-and-leave. F6 sessions do not save.

## Fall-start drop timing and landing bounce — follow-up, native/Windows acceptance pending

TreeHarvest still reserves deterministic rewards and final landing points before the felling proxy begins. The falling scene now calls the **idempotent release immediately in Begin**, rather than waiting for 90-degree contact. An interrupted proxy cannot double-release rewards. Each existing DroppedItem animates its original 0.55-second outgoing hop, then makes **two decreasing rebounds lasting 0.28 seconds total**, varying slightly by stable drop ID. Pickup is allowed only after the second bounce. A save/Continue during the fall, flight or rebound retains the exact source/drop/inventory outcomes and restores the cosmetic items settled, not re-flown. The existing scattered positions, chunk ownership, IDs and schema 3 are unchanged.

Tune the new exported `LandingBounceHeight`, `LandingBounceSeconds` and `LandingBounceCount` on the **existing** DropScatterProfile / DefaultDropScatter Inspector (defaults 8, 0.28, 2; Count 0 disables). No new profile files or scripts, physics bodies, extra processing while settled, or modifications to the fingerprinted DefaultDropScatter.tres. The H1–H4 fixture assertions have been revised for immediate release and a still-uncollectable bouncing item. **No Godot/.NET or graphical tests were performed during this GitHub-only change.** Manual F6: chop Oak; logs should appear while it first tips, touch down and bounce twice before E works. Check chunk-edge landings, saved midflight drops and F5 Continue.

## Ownership and authoring

PlayerInventory owns shared storage/hotbar models; DefaultPlayerInventory references canonical starting items. DefaultTreeHarvest owns tool capability, required work and DropTable; both species reference it. DefaultDropScatter owns presentation and capacity tunables. New item/resources can use these contracts without per-species reward logic; external Avalonia authoring itself is not implemented here.

WorldChunkStream records partial work and felled tombstones synchronously. WorldDropSpawner reserves deterministic rewards before final completion, indexes by final landing chunk, and retains records while offscreen Nodes are freed. Reload restores settled artwork; falling proxies preserve seasonal/wet/snow appearance with standing wind disabled. No idle harvest tick or global pickup scan was added. Only active falls/airborne drops animate.

Campaign schema 3 stores exact stack slots/keys/counts, shortcuts/selection/shield flag, source work/felling and remaining drops together. Saves during a fall or hop retain the committed final landing. Fully collected records are pruned; source tombstones prevent replay. Unknown item IDs fail restore; malformed references/owners reject before commit. Existing schema-1/2 campaigns get starter inventory once. A narrow hash-guarded bridge accepts the exact pre-H4 content graph, including compatible older bridges. New default-valued chunk fields omit serialization so old immutable blob hashes remain valid.

## Validation

Direct C# compilation passed with Godot source generators, net8 and available Godot 4.5.1 assemblies. Project remains on its authored Godot.NET.Sdk 4.7.2; a normal target-SDK build and Windows visual check were unavailable in this environment.

Actual World disk tests use dedicated storage and separate engine processes: write 33 checks; read 28 checks. They cover real confirmed axe contacts, partial work, cross-chunk landings, mid-fall snapshots, coherent collection-observer snapshots, offscreen node release, return/reload, equipment and inventory restore, and repeated Continue without grants/rerolls. Actual unchanged pre-H4 revision disk fixtures additionally pass 10 migration checks, preserving world identity/geography/removal/burn edits and leaving originals untouched.

After integrating the concurrent production water-default and plant-plan updates, real World write/read and actual-old-file migration checks passed again (33/28/10).

Related checks: harvesting 67, inventory 54, melee 51, HUD 45. P4 write/read repeated successfully with 44/39 checks. A first P4 run hit native Godot disposal failure; unchanged baseline also segfaulted at shutdown after its 44 checks. These native-runner failures are recorded rather than treated as proof of clean visual/runtime acceptance.

No stump/regrowth, falling collision/damage, shield item, H5 enemies/mining/containers or external editor UI is included.

Publication rebase: preserved concurrent PL0–PL3 plants and root backlog updates. PL3 intentionally advances the local scenery generator to v2, so previous v1 layouts now reject under that phase’s policy; the older-file migration results above describe H4 before PL3.
