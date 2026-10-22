# Resource-driven production trees

**Implemented:** every production tree uses `Shared/Tree.tscn` and the same `Shared/Tree.cs`. Species and artwork variants are Resources; adding content requires no species scene or C# script. Oak and Pink Blossom are migrated. Their obsolete production scenes were removed after a reference audit and runtime checks; independent DEBUG trees remain unchanged.

## Content contract

| Resource | Owns |
| --- | --- |
| TreeDefinition (`Oak/Oak.tres`, `PinkBlossom/PinkBlossom.tres`) | Stable ID/name, VisualVariants, hit config/enabled flag, wind material, snow foliage profile, CanBurn and fire profile; inherited spacing/environment/biome options remain available. |
| TreeVisualVariant (inline subresource or separate .tres) | Stable variant ID, texture, source-pixel root anchor, optional ground offset, positive sprite scale, sprite offset/centering, root-local trunk polygon, conservative visual bounds, optional spacing/wind/fire overrides. |
| TreeHitConfig | Wobble timing/strength, leaf/petal colour and count. Set LeavesPerHit=0 for leafless content. |
| TreeFireProfile | Burn rules, optional charred texture/anchor and canopy emission band. Missing charred artwork does not disable ignition, progression or GPU flames. |
| ShaderMaterial | Shared production sway shader and species amplitude/speed/canopy settings. Existing wind/snow adapters reuse shared variants by source/orientation/profile. |

TreeDefinition still inherits SceneryDefinition for biome compatibility; **do not populate its inherited PackedScene Variants**. Trees use VisualVariants. Rocks and decorative grass keep their existing scenery PackedScene path.

## Adding a species or variant (future Avalonia authoring contract)

1. Add artwork to a species `Sprites/` folder and create/copy a TreeDefinition .tres with a unique Id.
2. Add one or more TreeVisualVariant Resources to VisualVariants. Specify source-pixel SourceAnchor at the visible root, SpriteScale and the corresponding root-local TrunkPolygon. Centered/offset artwork and asymmetric mirrored anchors are supported. Sprite position is computed so the chosen source anchor lands at GroundOffset relative to the physics root.
3. Set conservative VisualMinimum/VisualMaximum around the rooted art. A variant can override spacing; placement never reserves less than its trunk radius. Population scale/mirror is applied after variant selection to fit tests, root collision and scenery exclusion.
4. Reference shared/configured hit, wind and fire Resources; set SnowResponse=Vegetation and the appropriate SnowFoliageProfile (existing green=0/pink=1). Optional disabled hit/fire capabilities need no scene changes. CanBurn=false disables ignition regardless of available artwork.
5. Reference this TreeDefinition from the biome's BiomePopulation with counts, scale/mirror chance and placement constraints. No new .tscn or scripts are needed. One species can be referenced by several biomes.

Variant selection consumes the existing seeded placement RNG once per requested object; Tree.cs performs no random selection. Configuration occurs before children enter/ready, so hit, fire, wind, snow and shadow caches all see the final variant and metadata. The world owns environment and fire registration after startup generation. Future streamed/dynamic content will need corresponding environment-cache registration; this change does not add streaming.

## Runtime modules

The template has stable Sprite, TrunkFootprint, HitResponse, Burnable and FireAppearance child paths. Hit wobble changes only the Sprite transform. Burnable owns simulated state; FireAppearance owns optional charred presentation and suppression/restoration of healthy foliage. Healthy texture/anchor are resolved from the selected variant and cached per variant/profile, without editing source resources or copying shaders per idle tree. If no burnt image exists, the original sprite remains and shared canopy-shaped flames still play.

All player controls remain in PlayerInput.cs. Trees and hit receivers have no idle update loops. The world fire manager uses generic burnable registration and the debug buttons say “tree”, with no species filtering. Existing fire spread is **not implemented**; this refactor preserves the current FIRE1/FIRE2 semantics.

Validation: C# build, full-density Godot 4.5.1 Compatibility rendering, both-biome runtime checks, both-species axe/wind/snow/fire, oak charred/reset alignment, original DEBUG startup, deterministic replay and a Resource-only third-species/two-variant fixture. See TESTS/Trees and TESTS/Fire. Windows renderer/performance needs a local check.
