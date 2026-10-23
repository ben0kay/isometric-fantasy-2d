# Feedback

Shared presentation-only systems, independent of damage and world location. [HealthBars](HealthBars/README.md) now provide shared, configurable, event-driven world-space HP presentation for player and entities based on the existing C1 `ActorHealth`. [Visibility](Visibility/README.md) is implemented. [DamageNumbers](DamageNumbers/README.md) implements C3.1's event-driven floating HP-loss labels (build/visual acceptance pending). CameraShake, HitFeedback and ScreenEffects remain future phases. See [C3 implementation notes](../COMBAT/C3_Implementation.md).

Presentational bars never own HP, defense, damage, or save state. Each actor binds its bar to its own health and sprite; no per-frame scans or global feedback manager.

[C3.3a+b Particles](Particles/README.md) now provides bounded shared CPU particle slots/profiles/shapes, used by hit-generated tree leaves. Tree-owned wobble stops independently of falling leaves. Ambient leaves and material-specific combat impacts remain separate. [Validation](../COMBAT/C3_3ab_Validation.md).
