# Live combat debug controls

Open F9 → Combat. Independent masters control enemy melee, knockback, stagger, impact particles, local separation and engagement positioning. ES3 sliders/optional-policy switches tune an in-memory shared engagement profile; explicit per-entity overrides retain their own settings. Direct mode opts out of positional spreading.

Closing the drawer/HUD does not reset the trial; Reset combat tuning or unloading the panel restores original settings. Nothing is saved to assets/campaigns. Widgets and practice counters refresh at 4 Hz only while this drawer is visible; the HUD does not change gameplay through its refresh callback.
