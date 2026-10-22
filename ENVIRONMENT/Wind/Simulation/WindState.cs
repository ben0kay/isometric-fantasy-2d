// Read-only snapshot of the one shared wind source, in world pixels and seconds.
using Godot;
public readonly record struct WindState(float Strength, Vector2 Velocity, Vector2 Displacement);
