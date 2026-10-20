# Player / Input

`PlayerInput.cs` reads `player_left/right/up/down` Input Map actions using Input.GetVector. Arrow bindings live in project.godot and can be remapped in Project Settings. The returned intent has length at most one; this component never moves physics bodies. Implemented, runtime validation pending.
