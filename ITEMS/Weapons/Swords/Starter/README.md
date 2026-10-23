# Starter sword — C2.5

`StarterSword.tres` supplies generic equipped-item data and independent primary sweep / alternate thrust Resources. Primary: 8 damage, 48 reach, 0.32s + 0.12s cooldown. Alternate: 6 damage, 62 reach, 0.24s + 0.3s cooldown. Both exclude prop hits, so the sword does not chop trees.

**Presentation polish:** the shared LMB wind-up no longer teleports backward; MMB uses a quick 32px visual jab rather than the inherited 14px. Physical reach/damage/cooldown remain unchanged. `placeholder_sword.svg` is deliberately simple temporary test artwork, not finished game art. Its grip/scale makes the blade point along local +X; replace through item data later. `2` selects it during starter testing. No separate sword scene/controller or inventory.
