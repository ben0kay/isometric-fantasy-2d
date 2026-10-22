# World Hostility, Wildlife, Shelter & Exploration — Design Ideas

> **Status:** Brainstorming / design direction only — NOT approved requirements or implemented systems.
> **Date:** 2026-10-21
> **Project:** Fully top-down 2D fantasy survival sandbox (despite repository name containing "isometric").
> **Current focus:** Establish the medieval/high-fantasy survival world first. A separate alien/sci-fi world is a possible future project or mode, NOT a firm decision.

## Design vision

Create a **fairly realistic, challenging, living survival world** with dangerous wildlife, hostile creatures and meaningful exploration. Daytime must NOT be a guaranteed safe period, and night must NOT simply activate a wave of monsters. The player can travel at any hour, with different risks and opportunities.

The key fantasy is: **nowhere is perfectly safe, but everywhere is potentially explorable with enough knowledge and preparation.**

## 1. Danger happens day AND night

Avoid Minecraft-style "monsters mainly spawn at night" logic. A bear can defend its den at midday; bandits can attack a traveler in daylight; a nocturnal predator may become more active after dark. No creatures should magically materialize solely because night started.

Time-of-day can influence:
- Activity and routines (diurnal, nocturnal, crepuscular, irregular).
- Hunting, feeding, sleeping, migrating and territorial patrols.
- Visibility and detection: darkness hinders the player and some enemies, but can also help stealth.
- Temperature, dampness and fatigue risk.
- Which rare resources, creatures or events become accessible.

Danger should also depend on biome, weather, local territories, player behaviour, sound, light, injuries, preparedness and encounters between creatures.

**Avoid global difficulty scaling based solely on days survived.** Dangerous locations can exist from day one; the player should learn to recognize and avoid them. Do not make every enemy auto-aggressive or auto-target the player.

## 2. Why shelter at night if exploration is allowed?

Shelter should be valuable for overlapping *practical* reasons, not because the game forbids night travel:
- **Exposure:** Lower temperatures, wind, rain, wet clothing and snow increase hypothermia/illness risk.
- **Visibility/navigation:** Poor visibility makes threats, obstacles and landmarks harder to notice.
- **Fatigue/sleep:** Sleep and recovery are possible outdoors, but can be poor-quality or interrupted if exposed.
- **Security:** Walls, doors and defensible positions reduce (but do not eliminate) danger from wildlife, intruders and environmental hazards.
- **Stored supplies and facilities:** Shelter gives a reason to create a base for food preparation, tools, crafting and long-term storage.

Fire and lanterns provide warmth and visibility, but may attract attention. Rain and wind may complicate fire upkeep. Shelters should be *safer*, never magically immune to attack.

Night exploration should have **rewards** too: stealth opportunities, different wildlife, rare nocturnal creatures and special resources/events. Prepared players may decide to travel instead of sleep.

## 3. Wildlife and enemy dispositions

Treat disposition as a **default tendency**, not an unchanging faction flag:

| Type | Examples | Typical behaviour |
| --- | --- | --- |
| Passive | Deer, rabbits, birds | Forage, wander or flee when threatened |
| Neutral | Boars, some large herbivores | Ignore people until approached, cornered or provoked |
| Territorial | Bears, nesting animals, guarding creatures | Defend den, offspring or home range |
| Predatory | Wolves, giant spiders, carnivorous dinosaurs | Hunt suitable prey; may target people depending on context |
| Hostile | Undead, cursed monsters, certain beasts | Frequently seek or attack enemies |
| Intelligent | Bandits, goblins, organized humanoids, potential robotic patrols | Assess threats, patrol, cooperate, flee, raid or negotiate where relevant |

Behaviour modifiers could include hunger, injury, fear, pack size, territory, young nearby, time of day and species-specific needs.

The player is **one participant in the ecosystem**, not the only target. Example: deer flee from a wolf pack, wolves react to a nearby bear, and the player chooses to avoid, observe or intervene.

## 4. Encounter and population model

Prefer believable world activity over endlessly spawning an enemy just off camera every few minutes.

**Three complementary sources:**
1. **Resident populations:** Dens, nests, camps, territories, settlements and cave inhabitants.
2. **Roaming activity:** Hunting predators, migrating herds, travelers, scouts, bandit patrols and wandering monsters.
3. **Special events:** Rare migrations, raids, invasions, unusual supernatural or technological phenomena.

Killing or driving away residents could temporarily make an area quieter. Recolonization should be possible but not instantaneous.

**Performance principle:** Fully simulate only nearby active entities. Represent distant populations, territories and schedules using cheap aggregate state. Use spawn budgets, active radius, pooling or capped updates as needed, and profile before implementing an elaborate ecosystem.

## 5. Biome-specific challenges and reasons to travel

Biomes should feel **distinct**, not simply have higher-level copies of identical enemies:

| Biome | Possible encounters | Distinct challenge |
| --- | --- | --- |
| Temperate forest | Deer, bears, wolves, bandits | Limited sightlines, animal territories |
| Swamp | Snakes, insects, amphibious monsters | Mud, standing water, disease risk |
| Frozen wilderness | Hardy predators, ice creatures | Cold, exposure, scarce food |
| Desert | Reptiles, raiders | Heat, thirst, limited water |
| Volcanic area | Ash creatures, heat-tolerant predators | Heat, fumes, hazardous ground |
| Prehistoric jungle | Dinosaurs, giant insects | Dense foliage, unfamiliar large fauna |
| Corrupted forest | Undead, mutated wildlife | Magical danger and altered ecology |
| Mechanical wasteland (possible future) | Robots, automated defenses | Sensors, patrols, machinery hazards |

These are **ideas**, not a required roster of starting biomes. The future world generator should emphasize strong visual biome variety rather than forcing long travel through multiple near-identical climate zones.

**Travel incentives:** Distinct ores, woods, plants, rare ingredients and animal materials; knowledge in ruins/libraries; traders and settlements; cave entrances; exotic wildlife; dynamic events; and unusual landmarks. Don't make every desirable item available immediately beside the starting base, but avoid repetitive arbitrary resource gates.

## 6. Player settlements and safety progression

A player might develop from temporary campfire/bedroll to lean-to, cottage, worksite, farm and eventually a defended homestead. Familiar territory becomes comparatively safer through knowledge and defenses, not by disabling danger.

Examples of natural nearby threats: predator tracks, damaged crops, animals attracted to stored food, bandits sighted on a road, or strange sounds from an adjacent biome. Avoid mandatory scheduled raids that punish builders every few days.

## 7. First-day experience (illustrative, NOT scripted)

- Early morning: The player spawns in a suitable wild area and gathers fallen twigs/branches.
- Daytime: They search for water and supplies; a boar, wolf, bandit or other threat may appear **before dusk**.
- Afternoon: They choose between making a fire and campsite, exploring toward smoke/ruins, or finding an existing shelter.
- Evening: Fatigue, cooler temperatures and lower visibility make the decision to rest or continue exploring meaningful.
- Night: Nearby creatures continue behaving according to species/activity patterns; darkness adds uncertainty, but players retain freedom to travel.

Different first-day survival stories should be valid; do not force every player through the same tutorial checklist.

## 8. Setting and long-term content direction — OPEN

The user is currently torn between **medieval/high fantasy with possible prehistoric influences** and an **alien/sci-fi planet**. They may ultimately build **two separate worlds**, one medieval fantasy and one alien/sci-fi, but want to see how the medieval world develops first.

**Recommendation for architecture, not worldbuilding commitment:** Keep shared systems generic (health, perception, needs, disposition, factions, spawning, AI, resources, environment modifiers) and data-driven so fantasy wildlife, dinosaurs, robots and aliens can use them later without requiring them all to coexist in the starting medieval world.

Avoid hardcoding world-wide assumptions like "all enemies are wolves or undead" or "all hostile creatures are nocturnal." Equally, **do not introduce aliens/robots/dinosaurs into the first playable medieval slice unless explicitly chosen later.**

## 9. Sensible implementation order (future)

1. Basic needs, shelter/exposure and day/night visibility consequences.
2. First passive wildlife and one neutral/territorial daytime threat.
3. Small species-specific behaviour state machine: idle, flee, warn, chase, attack, retreat.
4. Simple resident territories + limited roaming encounters; keep populations bounded.
5. Biome-specific habitat rules and variation.
6. Distant simulation, advanced pack behaviour, rare events and more content families only when warranted.

This note is for future planning only; there is **no request to implement it now**. Keep any preliminary prototypes isolated to DEBUG until selected for production.

## Core design principles to remember

- **Danger at every hour**; day versus night changes *how* danger is experienced.
- **Shelter is useful, not mandatory**; night exploration remains viable.
- **Passive / neutral / territorial / predatory / hostile / intelligent** are distinct starting behaviour models.
- **Wildlife interacts with wildlife**, not only the player.
- **Different biomes, different challenges**, not just larger stats.
- **Exploration rewards curiosity** through resources, encounters, caves and discoveries.
- **Realistic-feeling but enjoyable:** avoid chores and arbitrary player-targeted spawns.
- **Fantasy world first; sci-fi world optional later.**
