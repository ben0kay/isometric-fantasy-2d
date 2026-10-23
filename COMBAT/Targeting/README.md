# Minimal actor targeting

C2 `ICombatTarget` extends C1's receiving contract with an actor node, faction and hurt geometry. Both player and generic Entity implement it. `CombatTargeting` excludes self, same faction, dead, queued, removed, disabled, invalid-position and other-physics-world actors. A profile's receiving mask opts into Player / Hostile / Neutral. Actor factions are single values; the receiving mask may combine values.

Player is Player faction. `EntityDefinition.Faction` defaults Hostile and can be authored for other actors. Pursuit is still explicitly configured; merely having a faction does not activate AI. The axe may contact Hostile or Neutral; the default enemy profile targets Player. Richer hostility, factions, perception and A-Life are later work. No global registry or scene-root lookup.
