# Player

The playable character lives here. This is a **feature-owned folder**: player-specific code, artwork, sounds, and data belong with the player. Reusable systems needed by many actors should instead live in their appropriate shared systems (for example, `ENTITIES/Shared/` when it is introduced).

**Current status:** Folders and README planning notes only. No player controller, sprites, animation setup, movement, or gameplay scenes have been implemented here.

## Folder map

| Folder | Owns |
| --- | --- |
| `Input/` | Player controls, key bindings, and input intent (not physics). |
| `Movement/` | Movement/collision logic; future walk, sprint, jump, dodge and movement modes. |
| `Visuals/` | The player's appearance, facing, rendering, and animated presentation. |
| `Interaction/` | Player selection and interaction requests for nearby objects. |
| `Combat/` | Player-specific combat input/execution; damage rules should be shared. |
| `Stats/` | Player attributes, progression hooks, and player-specific stat configuration. |
| `Inventory/` | Player-specific inventory controller and hotbar; shared item/container rules belong elsewhere. |
| `Equipment/` | Player equipment slots and attachment presentation; reusable item definitions belong elsewhere. |
| `Audio/` | Player-specific sounds such as footsteps, breathing, and movement foley. |

## Blender-to-2D art pipeline (planned)

1. Author and rig a 3D character in Blender. Keep player-specific source material in `Visuals/Source3D/`.
2. Animate the 3D character and render individual frames or sprite sheets from consistent **isometric directions**.
3. Place final exported 2D frame atlases/sprite sheets in `Visuals/Sprites/`.
4. Create Godot animation playback resources (for example `SpriteFrames`) and directional animation mappings in `Visuals/Animations/` when implementation starts.

**Godot 2D bone rigs are not required for this pipeline.** The Blender rig produces the rendered images; Godot only displays and switches those images. Decide direction count, sprite resolution, character origin/feet alignment, and frame naming via a prototype before mass-producing assets.

## Existing configuration

- `PlayerMasterConfig.cs` — blank Inspector-editable resource type, created in the initial scaffold.
- `DefaultPlayerConfig.tres` — blank default configuration instance.

Add settings only as features are actually implemented; avoid duplicating global and per-feature values.

## Planned first test

Start with a small debug scene, controllable placeholder player, and independent camera. Prove movement, facing, draw order, and camera feel before extending to survival, combat, and world generation.

**Folder rule:** Keep small components with a clear single responsibility; do not create one node/script for every trivial helper or hard-code shared entity behavior inside the player.
