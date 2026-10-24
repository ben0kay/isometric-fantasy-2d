# Save migrations — compatibility gate implemented, conversions deferred

`SaveCompatibility` accepts durable schema 1, blueprint schema 4 and the current macro/feature/local generator versions. Earlier blueprints were session contracts, not shipped durable campaigns. Unknown versions fail explicitly and preserve source files. No migration that rerolls saved geography is permitted. A deliberate converter and content-upgrade policy must be authored/tested when a supported previous durable format actually needs conversion.

Current text-resource fingerprint changes also require an explicit migration; seed-policy changes alone do not. P4 compilation/runtime validation remains deferred to the local Copilot handoff.

## Explicit SO1 → SO2 layout compatibility
SO2 reorganizes only scene ownership and retains durable schema 1 / blueprint schema 4. References/SO1WorldScene.txt freezes the previous source scene text. CampaignSession accepts its saved fingerprint only if the current scene is exactly the known SO2 layout (SaveCompatibility.SO2SceneSourceHash) and all CURRENT dependency text still reproduces the previous fingerprint. This is not a blanket scene-hash bypass: changed resources or arbitrary later scene edits still reject. Continue preserves identity/map/edits; the next successful checkpoint uses the verified current fingerprint. No automatic file rewrite happens on menu verification. See SCENES/World/SO2_Copilot_Handoff.md; local migration/regression validation is pending.

SO2 validation update: Codex completed the Godot 4.7.2/.NET 8 build/import and relevant headless regressions, including real old-layout disk checkpoint restoration. All passed; P4 write/read now pass 39/33 checks after adding committed-migration assertions. See SCENES/World/SO2_Validation.md. Earlier automated-pending wording is historical; rendered seam/Y-sort and target-PC performance checks remain open.

GS1 ground-schema conversion: exact converted Oak/Pink text hashes gate substitution of frozen previous biome resources while all other dependencies retain strict fingerprint checks. Ordinary save checkpoints then use current fingerprint. See WORLD/Terrain/Ground/Surfaces/GS0_GS1_Validation.md; local disk-restore validation pending.

Accompanying-patch extraction: exact converted Oak/Pink text hashes gate frozen pre-extraction biome substitution, preserving strict checks for all other dependencies. Previous GS1 compatibility remains narrowly supported. See WORLD/Terrain/Ground/Surfaces/ACCOMPANYING_PATCH_LAYER.md; disk restoration still needs local verification.

GS5 optional Oak content: exact Oak/Pink/new Dirt/Rock Resource hashes gate frozen pre-GS5 Oak substitution. Historical dependency traversal excludes added profiles only for this explicit migration; all unrelated dependencies remain strict. See WORLD/Terrain/Ground/Surfaces/GS5_Validation.md; real disk Continue still needs local validation.

Procedural appearance revision: exact Oak/Pink/Dirt/Rock hashes permit previous full fingerprint reconstruction via Procedural*Before.txt source references. Earlier known terrain bridges remain gated; unrelated content still rejects. See WORLD/Terrain/Ground/Surfaces/PROCEDURAL_GROUND_RECIPES.md. Disk validation pending.

Dirt PNG appearance conversion adds exact-source-gated frozen Oak/Dirt references; all unrelated content remains fingerprinted. See ../../WORLD/Terrain/Ground/Surfaces/GS5_PNG_DIRT.md.
