# Biome identity colour

Implemented, awaiting native validation.

In each WorldBiomeDefinition, Identity now groups Id, DisplayName and Colour. Colour is the biome's target base-ground tone and the macro-map swatch. Macro generation no longer exports a separate OverviewColour. The old C# property remains an alias for source compatibility; production biome assets now serialize Colour.

Oak starts at (0.2196, 0.2745, 0.2078); Pink at (0.43, 0.265, 0.35), matching their existing source base tones. Their grass therefore receives neutral modulation and retains its current appearance. F7 now uses those same darker ground tones instead of the former independent bright map swatches. Artwork, accompanying grass patches, fine detail and optional layers keep their existing data.

To recolour a biome, edit Identity → Colour in its Biome.tres. The setup-time resolver converts the chosen RGB tone into modulation relative to the selected surface's BaseColour, then reuses the existing streamed tint map and preview uniforms. This avoids multiplying an already dark green by another dark green. For near-zero source channels, division is bounded at 0.001; unchanged channels explicitly yield 1. Colour is a reference tone, not a promise that every textured or lit pixel will equal the swatch.

The base layer's existing Tint remains a backward-compatible extra multiplier; leave it white when using Identity Colour as the sole colour control. Optional dirt and rock retain their own independent layer tints. Nothing writes into shared surface profiles. Omitted identity colours inherit the selected surface BaseColour, preserving legacy/fixture base appearance. Colours must be finite opaque RGB in 0–1.

Both streamed compiled palettes and the adjacent compatibility preview resolve WorldBiomeDefinition.GroundTint. MacroWorldBuilder copies Colour into MacroBiomeInfo. Appearance is compiled during world setup; there are no new frame callbacks, per-tree work, shader samples, texture uploads while idle or live Inspector listeners. Reload the world after authoring changes. F9 remains a session-only layer comparison tool.

Compatibility freezes the immediately preceding Oak/Pink resource text and gates substitution on the exact known colour relocation plus existing Dirt/Rock profile hashes. Older terrain and seasonal migration combinations reconstruct their previous full dependency fingerprints; unrelated content changes still reject. Geography hashing excludes colour already, so changing the swatch/tone does not reroll regions, scenery IDs or seed policy.

Reviewed source and static checks: production identity tones equal existing base tones; migration relocation reconstructs previous resource text; map uses Colour; streamed/preview use GroundTint; unchanged macro geography/hash algorithms and extra-layer Resources. Native tests added to ValidateGroundStack/ValidateGroundLayers cover target modulation, serialization, default grass neutrality, overlay isolation, map colour and geography stability, and the narrow compatibility bridge.

Not run: C# build, Godot import/headless scenes, campaign disk Continue, visual parity or target performance. The local execution environment is unavailable. Run TESTS/Terrain/ValidateGroundStack.tscn and ValidateGroundLayers.tscn locally, then F6 World/F7: inspect default grass and new map swatches, change a disposable biome's Identity Colour and reload, and confirm its grass/map tone change while optional dirt/rock remain independent.
