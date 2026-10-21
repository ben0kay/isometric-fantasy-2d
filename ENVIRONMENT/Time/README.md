# ENVIRONMENT/Time

The single source for in-game temporal state. **Clock/** owns hour-of-day/time speed and pause; **Calendar/** owns day/week/year, season scheduling and date information. Lighting and seasonal systems should consume this state, not each maintain their own clock. Future cross-system API should support save/load, fast-forward and deterministic replay.

> **Status:** Future architecture / folder placeholder only. No new production environmental mechanics are implemented in this folder.
