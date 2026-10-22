# Equipped sword visual test

Move the cursor to orbit/aim an equipped sword around the player. Left-click outside the comparison panels for a single clockwise 360° attack. Movement works during attacks; repeated clicks during an attack are ignored. The sword stays equipped and resumes cursor aiming after each swing. No enemies, hitboxes, damage, inventory or combat rules are added.

`SwordTest.tscn` is one optional instance under Actors/Player. `DebugSword.cs` owns cursor input and presentation; `WeaponMasterConfig.cs` / `DefaultWeaponConfig.tres` expose Enabled, HandAnchor, OrbitRadius, AttackSeconds and AimSpeed. The tiny pixel-style SVG is contained in Artwork/sword.svg here. Edit/replace that asset independently.

Cursor coordinates use the active camera's world transform. The weapon follows the player's snow/water visual offset after Surfaces updates; it stays visible above water/snow in this first equipped-item test. It draws behind the player for upward aim and in front for downward aim, while the actor root retains scenery Y sorting. Regular canvas lighting applies; no separate projected weapon shadow or animation frames. Cost is one sprite and a small per-frame transform calculation, without per-swing allocations, physics queries or generated textures.

Disable Config.Enabled to hide it or delete the EquippedSword instance and its ext_resource from TopDownVisualTest.tscn, then remove this folder. No other script depends on Weapons. Everything remains disposable DEBUG content.

Validation: all debug C# compiled against GodotSharp 4.7.2 / .NET 8 and the native Godot 4.5.1 source generators. Full-scene software OpenGL rendering was inspected. Runtime input checks passed for cursor aiming, active/completed spin, movement during attack, UI click filtering, up/down draw order, disabling, snow/water visual offsets and removing the sword instance. Rendering used reduced decorative grass counts locally only. Windows D3D12/hardware performance remains unverified.
