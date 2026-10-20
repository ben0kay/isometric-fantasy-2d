# WORK — Prototype 01: Fixed Isometric 3D + 2D Sprite Player

**Status:** Implementation committed; runtime validation pending.  
**Repository:** `ben0kay/isometric-fantasy-2d`  
**Scope:** A minimal *playable visual test* to evaluate our preferred art/technical direction. This is a **3D** test environment with a **2D SVG player placeholder**. Do not build survival, combat, chunk generation, inventory, quests, or any other unrelated systems in this job.

## Required reading — BEFORE touching code

1. Read **`/CodePreferences.md`** in full and follow the owner's C# conventions, modular folder organization, script headers, method comments/separators, regions, inspector export groups, clear names, major-folder README and MasterConfig preferences, performance awareness, and **one commit per job**.
2. Read **`/README.md`** in full for the latest game design. The current direction is a **fixed-angle, nonrotating 3D isometric world with 2D pixel-art-inspired character sprites**, with interchangeable visuals so models can be introduced later.
3. Inspect existing files/folders first (especially `CAMERA/`, `ENTITIES/`, `SCENES/`, `DEBUG/`). Reuse fitting empty scaffolding, README files and existing configs instead of creating duplicate systems.
4. Treat older README passages as historical if they contradict the updated 3D-with-2D direction. Do not copy 2D physics/CanvasItem gameplay systems into the 3D prototype.

## Goal / definition of done

When the Godot C# project launches, a **small, visually readable 3D test room / courtyard** appears from a **fixed isometric camera**. A visible **flat 2D SVG-derived player sprite** stands upright on the 3D floor. **Arrow keys move the player** along the ground, oriented intuitively to what the camera shows. The camera follows smoothly and never rotates from player input. Objects have 3D depth, visible shadows, and the scene clearly demonstrates a *sprite existing inside a 3D world*.

The scene is purposely attractive enough to judge the hybrid presentation; avoid a giant blank grey plane with an invisible capsule. But use **simple procedural/built-in geometry**, flat/pixel-inspired colors and a small, original SVG for now; no external asset dependencies.

## Pass 1 — Camera FIRST

- Add/organize a dedicated, reusable `Camera3D` controller under **`CAMERA/`**; C# for gameplay logic.
- Fixed isometric-ish orientation (e.g., approximately 45° horizontal diagonal and 35–45° downward viewing pitch). Camera orientation **must not change** with player movement. **No camera-turn input.**
- Start with **orthographic projection** for a readable isometric/pixel-inspired appearance, expose inspector settings for zoom/size, fixed viewing angle if helpful, follow smoothing and target offset. Make swapping to perspective later possible without reworking player movement.
- Follow the player smoothly on the horizontal plane. Keep framing stable (no judder, unexpected roll or automated angle rotation). Decide and document whether/how camera target height changes on slopes.
- Camera config lives in or is driven by a clearly named **MasterConfig** in its major folder where practicable; expose commonly tweaked values in logical Inspector groups. Do not overengineer configuration just for one scene.
- **Deliverable checkpoint:** launching test scene shows a correctly angled camera, even before the character is finished.

## Pass 2 — Small THREE-DIMENSIONAL test environment

- Add one clearly named debug/test scene under **`SCENES/`**, e.g. `IsometricVisualTest.tscn`; configure it as the runnable main scene, or include precise instructions to run it.
- A finite floor (e.g. ~24m x 24m) with **real 3D** physics collider; a lightly stepped/raised platform, a gentle ramp or slope, and some perimeter walls/columns. All are 3D meshes, not painted 2D isometric fake terrain.
- Include a few visually useful shapes: low walls, pillars, crates, maybe a rudimentary stone arch / doorway and a recognizable landmark. These show scale, occlusion and height differences. No Blender models necessary.
- Use coherent materials/colors for a stylized fantasy test (stone, dirt/grass, wood tones) with simple primitive meshes. Avoid depending on asset downloads.
- Add a `DirectionalLight3D` (sun), `WorldEnvironment` (ambient/background), cast/receive shadows and clear contrast so the player sprite can be judged. Basic fog is optional, keep performance reasonable.
- Ensure level bounds and geometry prevent the player from falling forever; walls and props intended as obstacles need accurate colliders.
- The player should be visible from spawn and have sufficient space to move. Clearly mark the spawn location.
- Organize scenery into neat scene-tree groups/helpers, not dozens of arbitrarily named children at the scene root.

## Pass 3 — SVG placeholder + 3D physics player

- Author an **original, simple, recognizable 2D SVG** player asset stored with player visuals (a stylized pixel-ish explorer silhouette, not a featureless blob), ideally with transparent background, readable head/body/feet and contrasting outline. SVG is a temporary art source; Godot may rasterize it on import. Set texture filtering/size so the result remains crisp, and document the import behavior.
- Use `CharacterBody3D` with `CollisionShape3D` for the gameplay body, and `Sprite3D` / `AnimatedSprite3D` child for the flat visual (no real-time 3D model). The sprite is visually **upright** in world space and **faces the locked camera** (configure once; no per-frame billboard rotation required if camera is fixed).
- Align sprite feet with the character's ground contact, with reasonable sprite scale and a visible/appropriate ground shadow (test and document Sprite3D lighting/shadow settings; add a cheap ground shadow indicator only if necessary).
- Separate player **input**, **movement**, and **visual presentation** into small clear components; place them under an appropriate **`ENTITIES/PLAYER/`** (or already-existing equivalent), not in a giant single player controller. Aim to allow replacing sprite with a `Node3D` model visual later without changing world-space movement/combat architecture.
- Use **arrow keys** as the required movement inputs (Godot Input Map actions, not hardcoded key polling in movement code). Map screen-up to screen-up on the isometric view, etc., using the camera's horizontal forward/right vectors projected onto the ground plane. Normalize diagonals so they are not faster. Handle gravity, grounded movement and jumping **only if necessary** to validate ramps; jumping is not part of this request.
- Choose a sensible initial walking speed, gravity and slope handling. Document inspector knobs for speed and visual scale. No sprint/dodge/combat.
- There is no need to produce four/eight directional animation sets now. A **single static SVG** is fine; movement direction is represented by moving the world-space body. Do not spend time building a full animation system.

## Pass 4 — Validation, cleanup and handoff

- Verify in Godot (and with a C# build where possible) that the scene loads, C# scripts compile, the SVG imports and renders as a 3D sprite, the arrows move correctly, diagonals are normalized, obstacles collide, the camera follows and **does not rotate**, and shadows/3D height are apparent.
- Check that the player doesn't visually drift above the floor, clip badly through objects or vanish behind default face-culling/lighting settings.
- Update/create concise README files for any touched major folders, and their suitable **MasterConfig** files according to `CodePreferences.md`. Keep the root README's status honest: **mark only what you actually implemented and tested**. Add a brief completion checklist and limitations to this `Notes/work.md`.
- **One GitHub commit for this implementation job**, with a clear message. Do not modify unrelated game systems.

## Suggested file ownership (adjust to existing repo before implementing)

```text
CAMERA/
  README.md
  CameraMasterConfig.cs        # or fitting existing MasterConfig naming
  IsometricCamera.cs
ENTITIES/
  PLAYER/
    README.md
    PlayerMasterConfig.cs      # or appropriate existing config
    PlayerInput.cs
    PlayerMovement.cs
    PlayerVisual.cs            # or thin Visuals/SpriteVisual implementation
    Player.tscn
    VISUALS/
      player_placeholder.svg
SCENES/
  README.md
  IsometricVisualTest.tscn
Notes/
  work.md                      # this task brief and completion record
```

**This is a suggested organization, not permission to duplicate existing structure.** Prefer simple C# components + scenes over a framework. If per-folder naming differs, stay internally consistent and document it.

## Decisions Codex may make without asking

- Exact camera pitch, orthographic size, room dimensions, primitive prop arrangement, SVG character design/colors, and inspector defaults.
- How to split the scenery into one or a few reusable test scenes.
- Whether to use a built-in primitive mesh or a tiny procedural mesh for a ramp.

## Explicitly NOT requested

- Real fantasy character rigs or sprite-sheet animation; Blender assets; camera rotation; a procedural overworld; biome/chunk streaming; deep cave generation; inventory; UI systems; enemies/combat; crafting; survival; save/load; major architectural refactoring; polish requiring external artwork.

## Recommended follow-up after this first visual test

Review screenshots and movement feel **before** choosing a detailed pixel-art pipeline. Next experiments can compare (a) a 2D sprite tree versus (b) a low-poly 3D trunk + foliage cards, and then prototype a small cave entrance. Future sprite-to-model upgrades must remain a swap of **visual representations**, not a rewrite of the entity's 3D gameplay logic.

---
*Authored 2026-10-20. Implementation record below distinguishes authored features from runtime validation.*


## Implementation record — 2026-10-20

- [x] Fixed-angle reusable camera, existing MasterConfig, orthographic/perspective options, exponential follow including height.
- [x] Authored 3D courtyard, terrain collider, ramp/terrace, walls, pillars/arch, crates, landmark and spawn marker.
- [x] Sun shadows and ambient WorldEnvironment.
- [x] Original SVG explorer in Sprite3D, upright fixed-camera alignment, nearest filtering, lighting/alpha-cut/double-sided shadows.
- [x] Separate Input, Movement and Visuals components in the existing mixed-case Player subfolders.
- [x] Arrow Input Map, camera-relative normalized movement, gravity, floor snap and slope handling.
- [x] Main scene and C# project scaffold; folder docs and root status updated.
- [x] Static checks of resource references, node paths, key codes and diagonal mapping.
- [ ] Restore/build C# and verify SDK matches installed Godot .NET version.
- [ ] Launch Godot and verify scene/SVG import, sprite visibility, feet, shadows and appearance.
- [ ] Play-test all arrows/diagonals, ramp ascent/descent, collisions and smooth nonrotating follow.

The session had no usable shell/Godot runtime, so no runtime checks or screenshots are claimed. Source review is not compilation. The SDK version follows project.godot's existing 4.7 feature declaration; local restore needs a matching Godot .NET SDK. No animation or obstruction fading. Raised props and walls intentionally occlude the player. Ramp uses a rotated box collider with a small overlap at the terrace seam; play-test this transition.
