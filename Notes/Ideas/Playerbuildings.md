# Player Buildings — Design Ideas

> **Status:** Future feature / planning notes only. **Do not implement yet.** Player-designed construction can be added later. These ideas are not final specifications and do not require changes to the current 2D debug prototype.

## Goal

Allow players to construct their own medieval fantasy homes and structures in the fully 2D top-down world: cabins, farmhouses, barns, workshops, taverns, forts, castles, and potentially settlements. Building should support varied footprints, materials, interiors, and eventually multiple floors, without requiring bespoke code for every structure.

## Core construction concept

- Modular placement of floors/foundations, walls, doorways, doors, windows, furniture, and roof materials.
- Prefer **walls on tile edges** so floor tiles remain usable; 32×32 tiles are a provisional starting point, not a locked dimension.
- Players define a footprint and rooms rather than placing only predefined whole-house objects.
- Building components can have material, durability, appearance, cost, and optional weatherproofing/support properties.
- Ghost previews, snap-to-grid, placement validation, construction costs, repairs, and dismantling are future UX considerations.
- Building gameplay should integrate with harvesting, mining, carpentry, masonry, crafting, farming, storage, and defense.

## Exterior appearance and roof visibility

**Desired presentation:** When the player is outside, a completed house displays its **full roof**, looking like a believable medieval building rather than a permanently cut-open dollhouse.

- Roof visuals are separate from floors, walls, furnishings, and collision.
- When the player walks through the front door into the building, the roof should **fade out or hide**, revealing the playable interior; restore it when the player exits.
- Consider triggers based on player location/room membership rather than relying exclusively on the door animation, so alternate entrances and teleports still work.
- Walls and upper floors may also need selective transparency/cutaway when they obstruct the player.
- Multiple nearby buildings should have **independent** visibility states.
- Roof visibility must not change the underlying structure, collision, or weather protection.

## Player-built roofs

Prefer a **hybrid automatic roof system** over manual placement of every roof pixel:

1. Player constructs walls and defines an enclosed footprint.
2. Game identifies roofable room/footprint regions.
3. Player chooses roof style/material (e.g. thatch, wood shingles, slate; possibly flat roofs).
4. Roof rendering assembles reusable 2D sprite segments: edges, corners, slopes, ridges, gables, and joins.
5. Roof can be repaired, replaced, or restyled independently from the rest of the building.

**Complexity note:** Rectangular footprints are simplest. L-shaped buildings, attached wings, courtyards, roof intersections, and irregular footprints require additional roof-shape rules. A future first implementation could support rectangles and joined rectangles before arbitrary polygons.

## Multi-floor buildings

**Keep multi-floor support in mind in the data model, even if it is implemented much later.**

- Potential levels: basement/cellar, ground floor, first floor, additional storeys, attic, roof.
- Each level has its own floors, wall edges, furniture, doors, windows, roof/ceiling state, and collision/navigation.
- Stairs, ladders, hatches, and ramps connect levels.
- The visible/interactive floor follows the player's current level; higher floors and roofs are hidden or cut away as needed.
- Characters and items should retain a **building ID + floor/elevation level**, so entities on different storeys do not incorrectly interact or collide.
- Consider whether the player can see downstairs through stairwells/holes, and how lighting, rain, sound, projectiles, and NPC navigation cross levels.
- Basements may use separate logical underground layers or scenes while still belonging to the same building.

## Room detection and interior rules

- Detect enclosed rooms from wall-edge boundaries and doors; rooms may change as players build, demolish, or open walls.
- Track indoor/outdoor classification, shelter, temperature/weather exposure, lighting, ownership, and potentially NPC occupancy.
- A large building can contain multiple independently detected rooms.
- Avoid tying room logic to a single decorative roof sprite; logical enclosure and visual roof generation are related but distinct systems.

## Suggested modular systems (future)

| System | Responsibility |
| --- | --- |
| Placement | Grid/edge placement, previews, costs, validity |
| Building pieces | Floors, wall edges, doors, windows, stairs, furniture |
| Room detection | Enclosures, room IDs, indoor/outdoor state |
| Roof generator | Roof footprints, styles, sprite assembly |
| Visibility | Roof fading, wall cutaways, floor selection |
| Multi-floor | Level coordinates, stair connections, navigation |
| Structural rules | Support, damage, durability, weather protection |
| Persistence | Save construction, materials, changes, damage |
| AI navigation | Doors, rooms, floors, building entrances |

All pieces should be **data-driven**, with reusable rules and visuals. The same building system could eventually underpin player structures and procedurally generated NPC buildings, even if NPC towns use authored templates.

## Future prototype when ready

1. Small test area with a **6×6-tile house**.
2. Place wall edges, floor, door, bed, and chest.
3. Generate a simple rectangular pitched roof with selectable material.
4. Walk through the door; roof fades to reveal the interior, then reappears on exit.
5. Validate independent visibility for two nearby buildings.
6. Later test two connected rooms, an L-shaped roof, and a staircase to a second floor.

## Open questions

- Completely unrestricted shapes vs rectangular rooms and modular extensions?
- How are support and construction progress simulated?
- Should roof generation be instant or require materials/work time per segment?
- How much of a neighbouring building's interior should be visible?
- Can players build on slopes, over water, or underground?
- Are upper floors simulated while off-screen?
- What happens to player buildings and their contents after permadeath?

**Reminder:** Preserve this as an idea document. No building-system implementation is authorized by these notes alone.
