# H0 — confirmed harvesting, ownership and release audit

Completed 24 October 2026 against the current production code; H1–H3 implementation/validation follows in [H0_H3_Validation](H0_H3_Validation.md).

## Confirmed contact and eligibility

Production: PlayerInput → EquippedTool → AttackExecution → MeleeHitQuery → TreeHitResponse. The old ToolHitQuery group scan remains a compatibility helper. IToolHitTarget.ReceiveHit is visual-only and unchanged. Optional IHarvestHitTarget receives HarvestHit with source, grounded hit position/direction and ToolWork snapshotted at attack acceptance. AttackExecution's optional argument preserves existing actor/enemy/direct-test callers. Work fields live on EquippedItemDefinition (HarvestTool, PrimaryHarvestWork, SecondaryHarvestWork); a fixture axe copy uses axe/1/0. No item-name/species branches are added to shared gameplay.

Harvestable snapshots accepted capability, required work and a compiled DropTable. It has no tick callbacks. Wrong tool/nonfinite work/finished or unavailable source does not progress. The final work request reserves all rewards before setting completion; denial retains prior progress and chosen quantities for deterministic retry. Visual hit counts/leaves never award work.

Tree state precedence: only Healthy Burnable + enabled hit receiver can harvest. Burning/burnt trees reject work. Final accepted felling disables tool feedback/standing collision, prevents ignition, and queues the standing source for normal teardown. The transient TreeFall sibling copies the rooted/mirrored sprite without standing shader/environment metadata; the physics root is never rotated. Normal TreeExiting cleanup remains authoritative for environment registries. Production tests cover the unchanged normal tree/fire/wind/snow/shadow path. Full live-environment integration during falling remains H4 graphical work.

## Logical reward and pickup ownership

SYSTEMS/Drops owns shared tables, deterministic rolls, a bounded scope coordinator and pickup presentation. SYSTEMS/Inventory remains the single storage authority. PlayerPickupInteraction subscribes to PlayerInput's player_interact action (E), only when explicitly enabled by the fixture. One request checks nearby 64-unit buckets and line of sight; no per-item overlap polling or full-world group scans. Collection uses all-or-nothing TryAdd before marking the record collected. Full packs leave items untouched; reentrant/repeated requests cannot duplicate grants.

Scope/source/completion IDs are explicit strings: `harvest-practice-v1/oak/0/fell/0`; each drop adds reward/pickup indices. No instance ID or global random state selects quantities/positions. Future production must substitute the existing WorldId and generated `world_instance_id` candidate identity. Drop records contain ID, event, item ID, quantity, launch/settled position, angles, release and collection flags; no Nodes. Reservation is one logical event, prior to fall; release cannot reroll. Interrupted standalone fall proxies release their already-reserved batch once. Scope teardown discards all disposable state.

WorldDropSpawner owns its logical records, bounded live/reserved count and child DroppedItem nodes under a Y-sorted actor hierarchy. Shared defaults cap 128 live pickups, 512 retained session records and 16 pickups per burst. Small 3–5-unit rewards split visually into individual pickups; larger quantities coalesce by item stack limit. Final-position circles, travel rays, bounds and a caller-provided water/terrain predicate reject inaccessible landing candidates in bounded trials. Population/position failure rejects the entire reservation. The coordinator has no timer or frame callbacks. Only airborne items process, then sleep; static shadows use ordinary canvas transforms. There is no expiry or deletion of uncollected items.

## Streaming, saves and release gate

P3 owns chunk nodes, `surface/x/y/content-id` candidate identity, removal tombstones and burn deltas. TreeExiting during a loaded chunk marks a removed prop and unregisters fire/environment/visibility; this is insufficient for harvested rewards. P4 fingerprints text scene/Resource dependencies and stores supported chunk/player/world state; it has no physical-drop journal or completed inventory integration. Existing InventoryStorage stack keys are session identities, not a durable equipment/save schema.

H1–H3 run only in TESTS/Harvesting. No automatic harvest attachment or production Resource tuning is added. Tree.EnableHarvesting explicitly rejects WorldScene ancestry until H4 is implemented. Production Oak/Blossom resources have no harvest profile; normal axe Resources retain zero work. Native checks verify the production content fingerprint remains unchanged by the fixture.

H4 must persist source progress/felled + chosen rewards and remaining pickup records atomically with durable inventory, map drop ownership by settled position to existing chunks, materialize only nearby nodes, handle cross-chunk landing/unload and legacy save policy. Do not use TreeFall/DroppedItem nodes as persistent state or infer rewards later from missing trees. A save during fall may restore a settled logical outcome, without replaying or rerolling cosmetics. Offscreen records must not keep Nodes alive. None of those H4 guarantees is claimed by H1–H3.

Historical H0–H3 evidence: production restrictions described above were superseded by [H4 World integration](H4_World_Validation.md).
