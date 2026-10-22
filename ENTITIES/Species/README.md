# Species — Creature content library

Organize by **what the actor is**, not whether it attacks the player. Each species has one canonical home; different roles, factions, dispositions and allowed habitats are separate data.

| Category | Examples |
| --- | --- |
| [Humanoids/](Humanoids/README.md) | Humans, elves, dwarves, goblins, orcs |
| [Undead/](Undead/README.md) | Skeletons, zombies, vampires, ghosts, liches |
| [Beasts/](Beasts/README.md) | Werewolves, trolls, dragons, giant spiders and stranger monsters |
| [Wildlife/](Wildlife/README.md) | Wolves, bears, deer, dogs, cows, birds, frogs, fish, sharks, dinosaurs |
| [Constructs/](Constructs/README.md) | Golems, potentially robots |
| [Otherworldly/](Otherworldly/README.md) | Possible aliens or non-native life much later |

## Example future species layout

```text
Skeletons/
├── README.md
├── Skeleton.tres        # Species definition (future)
├── Skeleton.tscn        # Reusable scene (future)
├── Visuals/             # Art, sprites, animations (when needed)
└── Variants/            # Basic / Warrior / Archer etc. (when needed)
```

Do not create all files just because a folder exists. Keep unique sprite/collider/animation information with the species; share locomotion, health, senses and combat through their owning central modules. Species definitions and variant resources can be referenced by overworld and cave biomes rather than copied there.

**All child species folders are placeholders.** The directories signal likely future content, not a commitment to implement every listed creature.
