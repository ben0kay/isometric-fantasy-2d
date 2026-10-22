# Clock
GameClock consumes ClockMasterConfig (StartHour, CycleSeconds, Paused). Hour wraps over 24 hours. SetHour supports paused scrubbing; every day/night consumer reads this node. No calendar/save integration is added.

P1-D: ManualRevision identifies explicit SetHour scrubs separately from continuous clock advance, allowing immediate sun refresh even below its pixel tolerance. Pause, wrapping and the default accelerated 30-second cycle remain unchanged. Inspector/F9 duration range now supports 5–3600 seconds (1440 = a 24-minute full day).
