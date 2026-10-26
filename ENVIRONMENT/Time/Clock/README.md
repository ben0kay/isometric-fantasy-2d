# Clock

> **SE4 / BC3 — 26 October 2026:** SE4: GameClock now publishes elapsed game-day fractions to one SeasonController. Calendar updates use that existing clock; SetHour never adds a day. Clock pause pauses calendar; weather retains its existing real-time effects. [Current handoff](../../Climate/SE4_BC3_Validation.md).

GameClock consumes ClockMasterConfig (StartHour, CycleSeconds, Paused). Hour wraps over 24 hours. SetHour supports paused scrubbing; every day/night consumer reads this node. No calendar/save integration is added.

P1-D: ManualRevision identifies explicit SetHour scrubs separately from continuous clock advance, allowing immediate sun refresh even below its pixel tolerance. Pause, wrapping and the default accelerated 30-second cycle remain unchanged. Inspector/F9 duration range now supports 5–3600 seconds (1440 = a 24-minute full day).
