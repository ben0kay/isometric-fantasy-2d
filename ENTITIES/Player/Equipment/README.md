# Player / Equipment

EquippedTool.cs temporarily attaches and animates the default axe through PlayerInput.ToolUseRequested. No inventory or slot system is needed. Tool scene/art/config live in ITEMS/Tools/Axes; shared hit query under Player/Interaction and tree response under WORLD/Content/Vegetation/Trees/Shared/HitResponse. All button input is centralized in PlayerInput.cs.
