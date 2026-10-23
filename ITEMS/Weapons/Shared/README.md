# Equipped item definitions

`EquippedItemDefinition` is reusable authored main-hand data: stable ID/name, primary and secondary `AttackProfile` references, held texture and grip/scale/orientation. Runtime selection and cooldown belong to the player adapter/executor, not this shared Resource. No inventory instance/durability state is implemented.

Axe and starter sword use the same held sprite and C2 executor. Add future items through data, not weapon-ID branches or copied attack controllers. Sweep/thrust presentation is selected by each attack profile; contact/damage remains in COMBAT.
