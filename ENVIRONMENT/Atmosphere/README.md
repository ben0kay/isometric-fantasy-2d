# Atmosphere
WorldAtmosphere retains bounded pollen/leaf/smoke/ember pools and reads shared daylight/wind. Fog/ holds haze shading; AtmosphereProfiles/ holds data, shared noise and organic sampling; AmbientEffects/ owns independent CloudShade.

AtmosphereProfile tunes switches, haze palette/density/scale and capped particle counts. Leaf emitters are cached from content metadata instead of tree names. Torch emissions consume the actual local fixture. Oak Forest supplies a local Atmosphere profile; other biomes can reuse these renderers. No UI lives here.


Ambient falling leaves cache the configured generic tree and hit receiver, using TreeDefinition.FoliageColour for the same colour as axe-hit bursts. HasFoliage=false and burning/burnt leaf suppression apply to ambient leaves too. No per-tree leaf colour Resource or per-frame tree search is needed.
