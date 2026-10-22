# Colour grading
WorldColourGrade and ColourGrade.tscn own only fullscreen contrast/saturation/vignette, copied from DEBUG Polish. ColourGradeProfile is editable/shared data, cloned per running world. The post layer is 10; developer controls render separately at layer 20.

## P1-B implemented — event-driven settings

ColourGradeProfile setters emit Changed only on actual edits. WorldColourGrade subscribes once and has no frame callback, enabled or disabled. Contrast/saturation/vignette uploads occur only when their values change; disabled grading hides the screen rect and defers uploads until re-enable. F9/Inspector edits remain live, and removal disconnects the profile event. The enabled fullscreen shader still renders normally and has GPU cost; no shader appearance or rendering algorithm was changed.

ValidateGradeTorchSleeping verifies unchanged enabled frames, individual edited uniforms, disabled edits, re-enable, never-enabled startup and unsubscribe. Local .NET 8 / Godot 4.5.1 checks are not a target 4.7.2 graphical/FPS benchmark.
