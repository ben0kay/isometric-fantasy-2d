# Sun shadows
SunShadowRenderer caches production sprites after scenery/wind setup. Shared silhouette materials preserve scale, mirror, offset and the sprite transform. Content data controls CastSunShadow and ContactShadowRadius; grass opts out. It updates projections from daylight and shared wind, never scans the scene each frame.

Art-directed flat-ground silhouettes do not occlude one another or climb terrain. More casters add draw/overdraw cost; dynamic registration is outside this fixed-world pass.

P3 stability: casters register/unregister with streamed chunk adapters. Cached conservative artwork reach plus current projection length rejects distant static casters before transform/material updates; off-screen casters remain active when their shadow can reach the prepared view. One camera rectangle is shared across the recursive renderer. Contact and projected nodes start hidden, disable interpolation and use world-space placement. Appearance changes invalidate extent caches. EnvironmentMasterConfig controls the common culling switch/margin; F9 reports total/relevant casters and CPU projection time. This supersedes the fixed-world/dynamic-registration limitation above. Native local-light shadows remain a separate system.

## P1-D projection cache and bounded solar movement

Static chunks skip caster walks when view, sun projection, opacity and source state are unchanged. Child renderers register through node lifecycle signals, replacing per-frame GetChildren arrays. Source visibility/rect/lifetime changes, appearance refresh and active TreeHitResponse transform events invalidate their owning group. No per-tree idle callback is added. Static content moved externally should still call RefreshAppearance; arbitrary static-body motion is not polled globally. Dynamic actors remain checked/projected continuously. Source subscriptions are removed on adapter unload; streamed new groups initialize even with paused time.

The day/night caller supplies a screen-pixel error budget (default 0.25, exact mode 0). Conservative artwork reach times projected-y-vector difference bounds world error; the canvas Frobenius norm converts the budget conservatively under rotation/zoom. Reused static geometry remains within the configured bound; source/settings/manual-time changes force a refresh. Culling uses the larger of requested/applied projection lengths. Visibility, contact shadows and opacity are updated independently. Shadow wind/softness uniforms only upload on changes, so paused wind still animates through shader TIME. Night silhouettes remain hidden; contact-shadow behavior is preserved. F9 diagnostics distinguish relevant casters from actual projection transform writes.

Validation: ValidateSunCaching covers paused solar/material caching, manual scrubs, profile changes, lightning, tree hit wobble, dynamic motion, zero tolerance, small/large movement, visibility/night/dawn, synthetic camera culling/re-entry and error under rotated/nonuniform zoom (20 checks). Scenery visibility/cancellation suite passed 167 checks; P3 streaming passed 4386 checks; generic tree checks reported 0 failures. F9/F7 smoke checks preserve independent panels. Local runtime .NET 8 / Godot 4.5.1; authored SDK remains 4.7.2.

CPU-only before/after probe: fixed seed, 367 cached casters, camera still, wind zero, synchronous simulated 120Hz updates, eight warm-up then 240 samples per state starting at noon. This is not a whole-day, walking, GPU or FPS benchmark. Values vary with the execution host.

| State | Before mean / p95 (ms) | After mean / p95 (ms) |
| --- | --- | --- |
| Paused | 1.37191 / 7.55940 | 0.01395 / 0.01380 |
| 30-second day, tolerance 0.25px | 0.97989 / 1.23430 | 0.36362 / 0.63140 |
| 1440-second day, tolerance 0.25px | 0.90570 / 1.17130 | 0.01997 / 0.02930 |
| 1440-second day, exact mode | 1.01780 / 1.62140 | 0.54775 / 1.25950 |

The original renderer had no tolerance; its paired slow-day rows are repeated baselines. Managed allocation during 240 synchronous updates was 3.81 MB in the steady baseline rows (7.14 MB first paused row) versus 0 measured bytes after. Enabled shadow shaders/overdraw, moving-actor checks, camera-change visibility scans and weather-opacity updates still cost work. Verify target-PC stepping, wobble, long shadows, zoom and GPU/frame timing before calling the user's lag/flicker resolved. The existing Mono 4.5.1 shutdown caveat remains applicable; no runtime shutdown fix is claimed here.
