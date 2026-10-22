// Placeholder for the shared production wind-state snapshot.
// =========================================================
// Planned concepts: direction as a normalized Vector2, speed, gust intensity,
// and resulting world-space velocity. Units/update cadence remain undecided.
// A separate immutable state type prevents consumers from owning the wind.
// No fields or runtime behaviour are defined yet.
public readonly struct WindState
{
}
