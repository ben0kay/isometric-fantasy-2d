# CAMERA

Fixed-angle 3D camera implemented in `IsometricCamera.cs`; runtime validation pending.
`DefaultCameraConfig.tres` uses `CameraMasterConfig` defaults: orthographic size 18, yaw 45°, pitch -35.264°, distance 24m and follow sharpness 9.

Assign Target and Config in the scene Inspector. Camera angle is set once at startup; there is no turn input. Follow uses exponential smoothing in render frames, including target height on slopes. Player physics runs at the fixed physics rate; physics interpolation is enabled project-wide to reduce judder. Projection/FOV can change in the config without changing player movement. Re-run after editing angle/config values.

Keep this camera outside the player hierarchy so it never inherits body movement or rotation.
