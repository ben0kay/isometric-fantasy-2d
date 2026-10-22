# Day/night illumination
WorldDayNight consumes GameClock.Hour, Weather rain/cloud/lightning and DayNightProfile. The original sunrise/noon/night palette and sun projection curves are preserved. No clock or UI lives here. SoftShadows/ShadowSoftness control cached materials through Shadows/.

## P1-D implemented — two-step sun caching

Step 1 caches the solar curves while Hour/profile are unchanged and skips identical ambient writes. Weather overcast and lightning remain live independently. Day/night retains a lightweight callback for moving actors, camera/culling, new chunks and changing wind. Paused time does not imply those systems stop. Profile Changed signals invalidate settings, and explicit GameClock.SetHour increments ManualRevision so even tiny developer scrubs refresh immediately on the next day/night callback. Pause/resume flushes any pending subpixel projection difference. Clock advance uses the same wrapped-hour equation but does not increment the manual revision.

Step 2 uses ShadowProjectionTolerancePixels (default 0.25 screen pixels; 0 = exact updates) to bound static projection reuse. Each renderer uses conservative cached artwork reach and a conservative canvas scale bound, including rotated/nonuniform zoom, to decide when the latest sun vector differs enough. It is not a fixed update cadence: fast test time can require every frame; a 1440-second day can reuse more frames. Opacity, visibility, soft-shadow settings and wind update independently, and moving actors always use current solar geometry. F9 exposes the tolerance for A/B comparison. The existing Seconds per day control/Inspector range now reaches 3600, including a 24-minute (1440-second) full day; the accelerated default remains 30 seconds.

ValidateSunCaching and BenchmarkSunShadows document lifecycle/error checks and measured local CPU results; see Shadows/README.md. No target 4.7.2 graphical smoothness/FPS certification is implied by headless checks.
