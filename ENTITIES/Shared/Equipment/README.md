# Shared enemy equipment presentation

`EntityEquipmentDefinition` holds sprite, scale, grip, hand offset and hold distance. Actual enemy definitions and artwork stay with their species/variant; player inventory items remain under ITEMS.

`EntityRangedWeapon` is R1’s optional bow canvas. The ranged intent helper supplies its aim/draw pose through the existing motor; it has no process callbacks. Only changed draw progress requests redraw. Bowstring/nocked arrow presentation never decides damage.

The Archer’s definition lives at `Species/Undead/Skeletons/Archer/Equipment/Bow/SkeletonBow.tres`, with artwork in its Sprites subfolder. Projectile flight/damage stay under COMBAT/Projectiles.
