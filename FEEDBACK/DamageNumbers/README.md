# Damage Numbers — C3.1 (planned)

> **Status:** Documentation/folder scaffold only. No damage-number scripts, Resources, visuals or runtime hooks have been implemented here yet.

## Purpose and ownership

Reusable world-space floating damage numbers for **player, generic enemies/wildlife and future damageable entities**. Own only visual presentation in `FEEDBACK/DamageNumbers/`; keep damage resolution in `COMBAT/Damage/` and authoritative HP in `ENTITIES/Shared/Health/ActorHealth.cs`.

Reference design from older project:
- `ben0kay/isometric-new-2d/VISUALS/Feedback/DamageNumbers/DamageNumbers.cs`
- `ben0kay/isometric-new-2d/VISUALS/Feedback/DamageNumbers/DamageNumberSettings.cs`
- `ben0kay/isometric-new-2d/VISUALS/Feedback/DamageNumbers/DefaultDamageNumberSettings.tres`
- `ben0kay/isometric-new-2d/VISUALS/Feedback/WorldFeedback.cs` and `VISUALS/Feedback/README.md`

**Adapt, do not copy old `COMBAT/Health.cs`.** C1 `ActorHealth` is the single source of truth in this game.

## Proposed files for C3.1 (do not create empty scripts prematurely)

```text
FEEDBACK/DamageNumbers/
├── README.md                         ← this scaffold
├── DamageNumbers.cs                  ← future event-driven display
├── DamageNumberSettings.cs           ← future [GlobalClass] Resource
└── DefaultDamageNumberSettings.tres  ← future inspector configuration
```

## Behavior

- Display **actual applied HP loss**, not raw request/base damage or armor-reduced damage prior to HP clamping. Eg axe 11 vs defense 2 produces `-9`; overkill displays the amount removed.
- Event-driven listening: C1 `ActorHealth.Changed(HealthChange)` gives before/after/restoration, and/or a single future applied-damage event at the shared C1 resolution boundary. Only emit on true damage (`Before > After && !Restored`), never on heals, save restore, blocked/immune hits, or max-HP initialization. If needed, add a minimal and nonduplicative event to the shared health contract. Avoid a second HP field and avoid coupling damage resolver to presentation.
- Brief pop, rise and fade; configurable size, outline, position offset, duration, colours, merge window, enable/disable. Reuse the old repo's *single label per actor / merge rapid strikes* approach initially, avoiding an unlimited number of spawned labels.
- Attach to the actor's rendered visual root / sprite bounds, using the same top-of-artwork anchoring considerations as `FEEDBACK/HealthBars`. Preserve readable orientation, YSort/z-order and appropriate camera zoom; ensure no unwanted mirroring on facing flips.
- Favor an optional shared binding helper only if it eliminates actual duplication between bars and damage numbers; no mandatory global feedback manager. Bar and damage-number settings/events must remain independently functional.
- Start hidden, `SetProcess(false)` while idle; wake only during an active animation, sleep at completion. Unsubscribe from health events on `_ExitTree`; dispose with actor streaming/unload. Cap/merge burst work.
- C3.1 is for **damage numbers only**, independent of knockback, hit particles, camera shake, parry effects or inventory. Works for future environmental/survival damage via changes to the same HP source.

## Acceptance / Codex validation

1. Inspect current HEAD, C2.5 results, `COMBAT/C2_Validation.md`, C1 Health and feedback bars before integrating. Use project CodePreferences.
2. Damage skeletal entity for actual HP loss, verify displayed damage and combined rapid-hit behavior. Repeat against player, if damage is enabled.
3. No number on heal, save restoration, zero/blocked damage; overkill number is clamped to actual HP lost. Confirm numbers on death as appropriate without unsafe access after actor despawn.
4. Loading damaged player/creature from P4 checkpoint must **not** spawn damage text. Avoid breaking P4 content compatibility.
5. Idle actors have no active numbers processing; numbers remain attached and clear on unload/death, no leaks; test multiple skeletons, player camera movement and streaming.
6. Compile Godot Mono .NET 8, headless import, targeted C1/C2/combat/feedback fixtures, and P4 save/read as relevant. Record actual results and any existing native-shutdown baseline failures.
7. Update this README and `COMBAT/C3_Implementation.md` only after verification. Separate visuals from damage rules.

The folder exists through this README; no runtime damage numbers are included in the scaffold.
