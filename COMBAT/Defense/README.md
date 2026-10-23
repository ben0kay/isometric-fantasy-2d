# Directional guard

C2.5 `DirectionalGuard` is a plain event-controlled defensive contract with no HP, callbacks or input dependency. `GuardProfile` authors reduction (default 60%), frontal half-angle (70 degrees), movement multiplier (0.45) and optional debug marker. Player equipment gates it behind an equipped shield flag; future actors can reuse the same contract.

`DamageRequest.Origin` is the optional world-space source location, supplied by C2 melee contact. Missing/nonfinite/coincident origins and rear contacts cannot receive directional mitigation. Guard faces toward the cursor. Dodge/release/death/scene suspension stops it immediately. The blue arc is a debug indicator, not shield art.

Order: base × attacker damage multiplier → subtract flat defense and clamp ≥0 → eligible guard reduction → round once away from zero → existing damage cap / remaining HP. Unguarded C1 arithmetic is unchanged. `DamageResult.Blocked` reports a guard that actually reduced the integer resolved damage; armor-only absorption is not reported as blocking. There is no independent shield HP, stamina, parry or invulnerability.
