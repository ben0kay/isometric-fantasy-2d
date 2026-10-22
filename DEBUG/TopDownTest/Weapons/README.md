# Equipped axe / tree-hit prototype

The sword test is replaced by an equipped axe. Aim with the cursor and left-click outside debug panels for one full-circle swing. Walk close to a trunk and aim toward it: one nearby tree wobbles and sheds ten extra leaves. Movement remains available during attacks. No chopping damage, health, harvesting, falling trees or fire are implemented.

## Modules for later extraction

- `DebugMeleeWeapon.cs`: cursor aiming, swing timing, equipped-art transform. No tree-specific code.
- `WeaponMasterConfig.cs` / `DefaultWeaponConfig.tres`: anchor, radius, attack duration, reach (52 world pixels from player feet to trunk edge), forward-sector half angle (65°), and contact progress (85% through the spin). These are visual-test tuning values.
- `WeaponHitQuery.cs`: once per swing, selects the nearest eligible `IWeaponHitTarget` in reach and the aimed sector. Targets register under `debug_weapon_hit_targets`; adding another response doesn't require teaching the axe about that object type.
- `AxeTest.tscn` / `Artwork/axe.svg`: replaceable equipped scene and contained pixel-style SVG.
- `../Trees/HitFeedback/`: separate tree receiver, wobble and local leaf pool/config.

Hit testing happens at the contact moment, using the current player position and aim captured on click. It checks the transformed physical trunk polygon, including scale/mirroring, not the leafy sprite rectangle. Misses and out-of-range swings do nothing; one swing hits at most one target. This prototype has no obstacle line-of-sight rule or continuous swept blade collider. Holding the button doesn't repeat; clicks while attacking are ignored.

Cost: one sprite; one group traversal and short polygon-edge checks per swing, no per-frame target searches or physics objects. UI input, scene sorting and snow/water offsets retain the prior equipped-weapon behaviour. Remove EquippedAxe from the player and the tree HitFeedback children to undo, then remove these debug folders and their references. The only coupling between weapon and receiver is the hit interface/group; production folders are untouched.

## Validation

All debug C# compiled against GodotSharp 4.7.2 / .NET 8 and native Godot 4.5.1 source generators. Full-scene software OpenGL rendering was inspected. Actual injected mouse input hit exactly one of two eligible nearby trees; distant and opposite-aim queries missed, repeated hits remained bounded, the trunk polygon stayed unchanged, snow/wind materials remained present, and feedback returned to idle/original transform after expiry and removal. Render checks reduced decorative grass density locally only. Windows D3D12 and hardware performance remain unverified.
