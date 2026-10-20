# Player / Movement

`PlayerMovement.cs` owns the CharacterBody3D root in Player.tscn. It projects the current camera's right/back axes onto XZ, limits diagonal length, applies configured speed and gravity, then calls MoveAndSlide. Floor snap and constant slope speed support the test ramp. No jumping, sprint or dodge. Camera projection does not affect movement mapping. Implemented, runtime validation pending.
