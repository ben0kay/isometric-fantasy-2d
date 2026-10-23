# Feedback

Shared presentation-only systems, independent of damage and world location. [HealthBars](HealthBars/README.md) now provide shared, configurable, event-driven world-space HP presentation for player and entities based on the existing C1 `ActorHealth`. [Visibility](Visibility/README.md) is implemented. [DamageNumbers](DamageNumbers/README.md) is scaffolded with C3.1 requirements and older-repo reference (not implemented). CameraShake, HitFeedback and ScreenEffects remain future phases. See [C3 implementation notes](../COMBAT/C3_Implementation.md).

Presentational bars never own HP, defense, damage, or save state. Each actor binds its bar to its own health and sprite; no per-frame scans or global feedback manager.
