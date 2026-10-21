# ENTITIES / Player

> **Status:** Production player folders and planning documentation only. The playable 2D debug explorer remains under [DEBUG/TopDownTest/](../../DEBUG/TopDownTest/README.md) and is **not** a production Player scene. This folder does not implement the actual player systems yet.

The production player stays within `ENTITIES/Player/`. Keep each player-specific concern in its own folder, while extracting mechanisms reused by many actors to appropriate shared modules. No separate root-level `PLAYER/` directory is planned.

## Folder map

| Folder | Owns |
| --- | --- |
| [Input](Input/README.md) | Player controls and normalized movement/interaction intents, not physics. |
| [Movement](Movement/README.md) | Walking, sprinting, collision, dodge, swim and other movement modes. |
| [Visuals](Visuals/README.md) | 2D sprite appearance, facing, animations and visual attachments. |
| [Interaction](Interaction/README.md) | Player intent, object selection and interaction detection; common object interactions live in SYSTEMS. |
| [Combat](Combat/README.md) | Player combat controls and state; shared damage/weapon/hit rules live in COMBAT. |
| [Stats](Stats/README.md) | Persistent player attributes, derived stat values and progression. |
| [Inventory](Inventory/README.md) | Player carried inventory and hotbar; item definitions live in ITEMS. |
| [Equipment](Equipment/README.md) | Player slots and visual display of worn/held items; clothing stats live in ITEMS. |
| [Audio](Audio/README.md) | Player-specific footsteps, breathing, injury sounds and foley; shared audio service lives in AUDIO. |
| [Survival](Survival/README.md) | Hunger, thirst, fatigue, stamina, temperature, health, injuries, conditions, wetness and sleep. |

## Survival folder

```text
Survival/
├── Needs/
│   ├── Hunger/
│   ├── Thirst/
│   ├── Fatigue/
│   └── Stamina/
├── Temperature/
├── Health/
├── Injuries/
├── Conditions/
├── Sleep/
└── Wetness/
```

**Separate concepts rather than parallel competing state:**
- **Stats** = base/effective attributes (e.g. strength, endurance, capacity); **Survival** = dynamic needs and physical condition. Do not create independent health totals that combat and survival disagree about.
- **Stamina** = short-term exertion; **Fatigue** = persistent tiredness. Hunger/thirst are separate survival needs.
- **Ambient temperature** is determined by ENVIRONMENT and climate/biome/shelter; player **body temperature** is simulated in `Survival/Temperature/` based on exposure, wetness, exertion and equipment.
- **Worn clothing** lives as canonical item definitions under `ITEMS/Equipment/Clothing/`, with insulation/breathability/protection; `Player/Equipment/` handles what is currently worn.
- **Conditions** represent player-facing states; reusable timed buffs/debuff implementations will be shared via `SYSTEMS/Effects/`.

## Future Blender-to-2D workflow

You may author/rig in Blender and render sprite directions/frames into `Visuals/Sprites/`, while retaining `Visuals/Source3D/` as an optional source-art location. Godot gameplay stays fully **2D top-down** with ground/feet sorting anchors, not a 3D/isometric camera. Animation sheets and rigs have **not** been added.

## Configuration

- `PlayerMasterConfig.cs` and `DefaultPlayerConfig.tres` are **blank, inspector-editable placeholders** for the future production player. The obsolete 3D script reference and `SpritePixelSize` field were cleaned up during this folder pass.
- `DEBUG/TopDownTest/` owns its own temporary movement, camera and visual config. No production Player script is currently wired to the debug scene.
- When implementing a mechanic, add settings to the owning data/resource class; avoid a large universal master config that duplicates per-feature fields.

**No survival mechanics, player movement code, item behavior, or production scenes are created by this reorganization.**
