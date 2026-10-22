# Cloud shade
CloudShade is an independent world-space overlay with CloudShadeProfile, shared atmosphere noise, shared wind displacement and daylight. It is separate from fullscreen colour grading and configurable per biome through profile references.

## P0-D lifecycle

CloudShadeProfile.Enabled=false or Opacity<=0 hides the overlay and stops its callback immediately. Changed events from both profile setters wake/re-evaluate it directly, preserving F9 controls. Re-enable applies current wind displacement/daylight/opacity once, so paused clouds do not restart from stale positions. While active, cached inputs skip unchanged shader writes. Active moving wind/daylight still require a shared callback; setup geometry/noise/material resources remain cached while off. Exit disconnects the profile event. No cloud look, weather scheduling or scene ownership changes are introduced.

ValidateCloudWindCaching checks never-enabled startup, off/zero opacity, settings edits while off, re-enable, live daylight/wind and cleanup. Local .NET 8 / Godot 4.5.1 lifecycle checks do not establish target 4.7.2 graphical FPS improvement.
