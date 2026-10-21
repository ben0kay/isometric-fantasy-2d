# Future Mechanic Ideas — Survival, Food, Equipment and World Realism

> **Status: IDEAS / DESIGN REFERENCE ONLY — DO NOT IMPLEMENT YET.**
>
> This note captures the October 2026 discussion about making the new **fully 2D, top-down medieval fantasy survival sandbox** feel believable and interconnected. It is a backlog of concepts, **not** an approved implementation roadmap, balancing specification, folder-creation request, or list of features already present in the project.
>
> **Important:** The user explicitly wants to **keep this as a note for now**. Do not add new foundation folders, components, scripts, resources or gameplay systems based solely on this document. Confirm scope when a feature is requested later.

## 1. Core vision / design goals

- Survival should require sensible preparation rather than being solved by collecting enormous stacks of permanent food (e.g. 60 apples that never spoil).
- Systems should meaningfully affect one another: climate, seasons, clothing, shelter, health, food, fatigue, encumbrance and travel.
- Farming, fishing, hunting, cooking, food preservation, crafting, repair, storage, and home-building should have practical survival value.
- Favour **believable but enjoyable** realism: interesting player decisions, not dozens of micromanagement bars or punishing hidden requirements.
- Keep mechanics **data-driven, configurable, modular and performant**. Reuse definitions/behaviour across player, creatures, containers and world objects where sensible.
- The current folder foundations are at `WORLD/`, `ENVIRONMENT/`, `ENTITIES/Player/`, `ITEMS/`, `SYSTEMS/`, `CORE/`, `COMBAT/`, `SAVE/`, `UI/` and `AUDIO/`. The proposed ideas here do **not** imply extra folders must be created now.
- Plan data in a way that a future **Avalonia toolbox** could inspect/tune items, food properties, spawn rules and effects using the same authoritative definitions as Godot.

## 2. Food, spoilage, cooking and nutrition — major priority

### 2.1 Perishable food and freshness

- Different foods have different shelf lives. Fresh produce, meat, fish, cooked dishes and preserved foods should deteriorate differently.
- Conceptual states: **fresh → aging/stale → spoiled → rotten**, with gradual changes in condition, quality, benefits, smell, and possible health risks.
- Spoilage depends on environment and storage: hot summer weather accelerates it; cool cellars can slow it; refrigeration/freezing (if ever appropriate) changes it further.
- Inventory **batches must retain distinct freshness**. Ten apples collected today should not magically receive the same freshness as ten harvested next week just because they were put in one container.
- A food's decay must continue across save/load and while its chest/world chunk is unloaded; avoid updating every food item every rendered frame.
- Keep realistic handling approachable: no need for a precise real-world pathogen simulator.

### 2.2 Preservation and storage

Ideas: **drying, smoking, salting, pickling, fermenting, making jams, preserving or storing in a cellar**. Each can have different ingredient/fuel/tool needs, processing time, nutrition/quality effects and shelf-life benefits.

Storage location matters: carrying perishable food in a hot backpack versus a cool underground cellar should make a difference. Food planning for winter or long expeditions should be worthwhile. Eventually shared containers (backpacks, chests, cupboards, barrels, cellar storage, carts) can have storage conditions such as ambient temperature and exposure.

### 2.3 Nutrition and a balanced diet

The player should benefit from variety rather than satisfying all nutrition with one plentiful food. Suggested **broad** nutritional properties:

| Nutritional concept | Typical sources | Possible purpose |
| --- | --- | --- |
| Protein | Meat, fish, eggs, legumes | Recovery and general physical condition |
| Carbohydrates | Bread, grains, roots, fruit | Activity energy |
| Fats | Nuts, oils, fatty meat | Energy reserve |
| Micronutrient/variety score | Vegetables, fruit and varied meals | Long-term general wellness |
| Hydration | Water, soups, drinks | Fluid needs and heat stress |

Avoid a giant list of vitamins unless it clearly adds fun. **Balanced eating should grant gradual, modest benefits**, not rapidly punish an otherwise healthy player for eating bread for a short period.

### 2.4 Cooking, recipes and meal quality

- Going beyond `Raw Meat → Cooked Meat`: combine ingredients and preparation methods into real meals (e.g. meat + vegetables + water + herbs → hearty stew).
- Meals can restore hunger/hydration, satisfy nutrition categories and optionally provide warmth, comfort, morale, recovery or other **temporary, modest effects**.
- Recipe definitions should refer to canonical ingredients/items and reusable cooking operations, not require a bespoke script per meal.
- Cooking has meaningful relationships with fuel, cooking equipment, time, ingredient freshness, storage, spoilage and serving portions.
- Some preservation steps should transform an item into another defined item or change its condition in a data-driven way.

## 3. Inventory, backpacks, encumbrance and transport — major priority

- Distinguish **weight** from **volume/storage space**. Feathers can be bulky but light; coins compact but heavy; logs and long weapons may have special carry limitations.
- Pockets, belts, pouches, backpack compartments, hands, equipped slots, containers and carried bulky objects can have different capacities or restrictions.
- Backpack type/quality determines available storage but **does not erase physical load**: carried weight still affects fatigue, stamina drain and movement.
- Effects scale gradually: comfortable load → burdened → heavy → overloaded; potentially slower running, harder dodging, higher stamina usage and long-distance exhaustion.
- Worn gear, weapons, wet clothing and carried containers contribute to actual load; strength/endurance or relevant skills may affect carrying tolerance.
- Potential future logistics: wheelbarrows, pack animals, carts, storage chests and transporting heavy building/mining materials.
- Prevent infinite inventory stacks by defining item weight, volume, stack limit and special carrying constraints.

## 4. Clothing, armour, equipment condition and repair

### 4.1 Clothing and thermal protection

- Items can provide **insulation, wind resistance, waterproofing, breathability, coverage and weight**.
- Clothing may be layered; a warm fur coat helps in a snowy biome but may cause overheating in a desert, especially during exertion.
- Wet clothing loses comfort/insulation or takes time to dry; snow, rain, immersion, fire and shelter influence the rate.
- Armour provides defense but may be heavy, restrict mobility, trap heat or become uncomfortable during long travel. Balance protective value against fatigue/temperature effects.

### 4.2 Durability and maintenance

- Weapons dull or break; axes/picks wear down; boots, backpacks and clothes tear; armour gets damaged.
- Material, quality, use, exposure and repair affect condition. Damaged equipment can work less effectively.
- Repairs, sharpening, tailoring, blacksmithing and crafting replacements provide long-term progression and professions.
- Item **definition** is distinct from a particular **item instance's condition**, which may be unique and persisted.

## 5. Player physiology and health

Already planned under `ENTITIES/Player/Survival/`: **hunger, thirst, fatigue, short-term stamina, body temperature, health, injuries, conditions, sleep and wetness**.

- **Fatigue versus stamina:** fatigue is long-term exhaustion; stamina is short-term exertion used by sprinting, combat, chopping or hauling.
- **Body temperature versus ambient temperature:** biome, season, time of day, wind, rain, shelter, campfire, wetness, activity and clothing all influence heat gain/loss. Symptoms/effects could include shivering, hypothermia, sweating, heat exhaustion and reduced performance.
- **Injuries:** bleeding, cuts, fractures, impaired mobility, wound dressing and gradual healing. Keep one consistent health/damage source rather than a competing separate HP system.
- **Illness and infection:** contaminated food or drinking water, exposure, dirty wounds, fever and recovery; medicine, rest and hygienic conditions could help.
- **Sleep:** safe, sheltered, warm bedding should produce better recovery than lying exposed outdoors; rest quality depends on weather, nearby danger, comfort, injury and sleeping conditions.
- **Hygiene:** mud, dirt, blood, washing, wet clothes and infection likelihood, if useful and not overly tedious.
- **Mental wellbeing:** fear, isolation, stress, comfort, security and morale, especially around long exploration, frightening biomes and well-equipped homes. Treat as optional until gameplay benefits are clearer.
- Avoid tracking excessive independent survival meters where a few combined, well-explained effects would work better.

## 6. Shelter, structures, room conditions, fire and fuel

### 6.1 Shelter and room warmth

- Buildings should have practical **weather protection**, not just decorative walls and roofs.
- Future room/enclosure logic can identify **indoors vs outdoors**, roof coverage, wind blocking, heat retention, comfort and possibly leaks.
- Construction material and wall/roof quality may affect insulation. A cabin with a fireplace should be much safer in winter than an exposed campsite.
- Cellars may provide natural cooler conditions for storing food; beds and bedding could increase comfort/recovery.
- Keep simulation rules separate from roof-rendering and roof-cutaway effects.
- Refer to [Player Buildings](Playerbuildings.md) for existing long-term building concepts (roofs, rooms, multi-floor houses).

### 6.2 Fire and heating

- Fireplaces, campfires, torches and stoves consume fuel over time and provide **heat and/or light**.
- Wet wood may burn poorly; fuel types differ in burn duration, intensity and smoke.
- Embers cool gradually; rain, wind and exposed placement change fire behavior.
- Fire can support cooking, drying clothing, warmth and nighttime safety; uncontrolled spread or smoke could become future hazards.
- Audio/particles/light are shared effects; avoid one-off fire shaders and timers for every object.

## 7. World, travel and ecological realism

| Mechanic | Possible gameplay value | Priority for future consideration |
| --- | --- | --- |
| **Water quality** | River, well and swamp sources vary; boiling or filtration makes safer water | High |
| **Terrain travel difficulty** | Mud, snow, slopes, thick vegetation, deep water and load affect walking | High |
| **Resource regrowth** | Trees, berries, plants and ore availability respond to time/harvesting | High |
| **Equipment material properties** | Wood flammable, cloth absorbent, metal conductive, stone durable | Medium |
| **Hygiene and disease risk** | Mud, exposure, water, wounds and washing | Medium |
| **Mental wellbeing** | Morale, comfort, stress and fear | Medium |
| **Wildlife ecology** | Predator/prey, grazing, migration, reproduction and seasonal population changes | Later |
| **Noise/disturbance** | Chopping, mining, fighting or loud campfires attract attention | Later |
| **Scent** | Food, cooking, blood or animals influence some creature detection | Later |
| **Building deterioration** | Roof leaks, weathering, structural damage, repairs and maintenance | Later |

Examples of desirable gameplay loops:

- **Winter preparation:** harvest crops → cook/preserve food → store it in a cool cellar → repair warm clothing → stock firewood.
- **Mountain expedition:** check weather → equip warm waterproof layers → balance backpack load → pack long-lasting food and safe water → plan overnight shelter.
- **Building a house:** harvest logs/stone → transport limited loads → build weatherproof walls/roof → create a warm room → use a cellar and bed.
- **Hunting/fishing:** collect highly perishable meat/fish → cook, preserve or share it rather than carrying it indefinitely.
- **Risk/reward:** long mining/woodcutting sessions may generate noise; water availability does not automatically mean water safety.

## 8. Cross-system data and performance foundations to remember

These are **architectural considerations**, not directions to write new code yet:

1. **Canonical definitions vs individual instances.** An `Apple` definition (weight, base spoilage parameters, nutrients) differs from a harvested batch (condition, timestamp, storage state). Likewise a `Sword` definition differs from one sword's durability/quality.
2. **Stable IDs.** Use durable IDs for items, biomes and content rather than relying on filenames or paths. Keep the data future-editor-friendly.
3. **Shared elapsed game time.** Read from the authoritative `ENVIRONMENT/Time/` clock/calendar. Recalculate slow changes from elapsed time rather than running hundreds of per-item frame callbacks.
4. **No per-frame item spoilage.** Prefer on-access/on-transfer calculations, periodic batched work or cached state as appropriate, including unloaded chunks and containers.
5. **Unified modifiers/effects.** Hunger, clothing, disease, encumbrance, weather and combat should use reusable ways to apply speed/stamina/temperature modifiers without multiplying mutually unaware subsystems.
6. **Clear ownership boundaries.** The world generates an Oak tree; harvesting yields an Oak Log item; ITEM definitions own item attributes; player inventory owns carried state; ENVIRONMENT supplies weather; player survival consumes environmental inputs. Shared gameplay processes live in SYSTEMS.
7. **Persistent state and save compatibility.** Item instances/batches need condition/timestamps; containers and world changes need save/restore. Decide the save/permadeath policy before committing to a final schema.
8. **Avalonia compatibility.** Later, allow the editing toolbox to inspect/edit content, nutrition/spoilage and biome/environment settings without maintaining an independent, conflicting database.
9. **Keep complexity proportional.** Batch related checks, avoid every item/plant updating each frame, and profile once dense worlds and many inventories exist.
10. **Tuneable gameplay, not rigid realism.** Keep weights, decay rates, status thresholds, climate modifiers, buffs and penalties adjustable from data/config rather than scattered constants.

### Possible module ownership (reference only; do not create new folders yet)

| Concern | Existing/planned owner |
| --- | --- |
| Item definitions, food stats, condition and physical dimensions | `ITEMS/` |
| Player carrying state, equipped slots and movement response | `ENTITIES/Player/Inventory/`, `Equipment/`, `Movement/` |
| Player nutrition, hunger/thirst and physiology | `ENTITIES/Player/Survival/` |
| Food processing/preservation and shared container mechanics | Potential future categories under `SYSTEMS/` |
| Shared timed effects/status application | `SYSTEMS/Effects/` |
| Region/season/time/weather/wind | `ENVIRONMENT/`, with climate profiles/restrictions in `WORLD/Biomes/` |
| Room/shelter/building mechanics | `SYSTEMS/Building/` and `WORLD/Content/Structures/` |
| Slow simulation scheduling | `CORE/Simulation/` |
| Item, player, container and world persistence | `SAVE/` and `WORLD/Runtime/Persistence/` |

These paths are **candidates for future implementation**, not a request to create new directories or move existing ownership.

## 9. Suggested order *when development resumes*

1. **Item-state design first:** distinguish type definitions from instances/batches; settle weight, volume, freshness, durability and identifiers.
2. **Inventory capacity and storage:** backpack/slot/carry limits, encumbrance and container conditions.
3. **Basic food survival:** hunger, thirst, broad nutrition categories, spoilage, simple cooking and preservation.
4. **Environmental exposure:** climate → clothing/wetness → body temperature, plus stamina/fatigue and straightforward status effects.
5. **Shelter and fire:** indoors vs outdoors, bed comfort, fire fuel/heat, cellars.
6. **Broader world consequences:** illness, water contamination, resource regrowth, animal ecology, noise, hygiene and building deterioration as they prove worthwhile.

This order is a **discussion aid**, not a requirement to implement all steps or prioritize realism over the playable 2D prototype.

## 10. Questions intentionally left open

- How granular should nutrition be: broad food groups or detailed macro/micronutrients?
- Should spoilage use continuous quality values, stages or both? How should damaged/partially used stacks merge?
- Should inventory be strict by weight + volume, a slot-based hybrid or weighted encumbrance with soft limits?
- How much equipment layering, maintenance, hygiene and medical realism improves fun versus micromanagement?
- How quickly do seasons pass, and how much seasonal simulation runs in inactive world regions?
- Do campfires spread naturally; how are shelter/temperature simulated without excessive CPU cost?
- What should persist when the player dies and begins a new run?

---

**Related notes and foundations:** [Player buildings](Playerbuildings.md), [project vision](../../README.md), [player survival](../../ENTITIES/Player/Survival/README.md), [world](../../WORLD/README.md), [environment](../../ENVIRONMENT/README.md), [items](../../ITEMS/README.md), [core simulation](../../CORE/Simulation/README.md).

**Reminder:** This file is a **future ideas note only**. No new gameplay systems, scene wiring, foundation directories, definitions, or test features were requested or implemented in this pass.
