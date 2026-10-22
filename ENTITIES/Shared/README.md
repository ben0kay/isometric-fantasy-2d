# Shared entity mechanics

Reusable *actor-owned* components live here. Examples: senses, state-driven AI, navigation interfaces, movement modes, health state and species/variant definition schema. Every species opts into the components it needs; an ambient-only particle bird needs none.

| Folder | Intended ownership |
| --- | --- |
| [AI/](AI/README.md) | Behaviour/state decisions; flee, patrol, chase, idle and group decisions |
| [Navigation/](Navigation/README.md) | Shared path requests, obstacle handling and route-following |
| [Perception/](Perception/README.md) | Vision, hearing, detection radii and target awareness |
| [Movement/](Movement/README.md) | Reusable land/swim/fly movement capability modules |
| [Health/](Health/README.md) | Actor-owned health state, death notification and damage response |
| [Definitions/](Definitions/README.md) | Future data contracts for species, variants, dispositions |

Cross-cutting **damage calculation**, equipment/item definitions, general gameplay services and biome spawning should stay in `COMBAT/`, `ITEMS/`, `SYSTEMS/` and `WORLD/` respectively.

**Status:** Folder documentation only; build concrete modules when gameplay needs them, not as speculative frameworks.
