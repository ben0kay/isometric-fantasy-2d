# H4 — real World harvesting

Implemented 25 October 2026. H5 remains planned. Windows visual/performance acceptance is pending.

## Play in the real world

Run normal F5 → New Campaign/Continue, or F6 `SCENES/World/World.tscn` for a disposable World. Select starter axe with 1; land four LMB chops on Oak or Pink Blossom. The rooted artwork falls, then 3–5 Wooden Logs scatter/hop and settle at varied angles. E collects a nearby settled log into actual Player storage; I shows counts. 2 selects sword; 1–9 selects shortcuts. G toggles the temporary shield (no shield item yet). F8 saves a campaign; Esc supports save-and-leave. F6 sessions do not save.

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
