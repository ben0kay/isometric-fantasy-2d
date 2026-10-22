# Clock
GameClock consumes ClockMasterConfig (StartHour, CycleSeconds, Paused). Hour wraps over 24 hours. SetHour supports paused scrubbing; every day/night consumer reads this node. No calendar/save integration is added.
