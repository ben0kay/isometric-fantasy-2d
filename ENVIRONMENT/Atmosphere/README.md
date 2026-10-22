# Atmosphere
WorldAtmosphere retains bounded pollen/leaf/smoke/ember pools and reads shared daylight/wind. Fog/ holds haze shading; AtmosphereProfiles/ holds data, shared noise and organic sampling; AmbientEffects/ owns independent CloudShade.

AtmosphereProfile tunes switches, haze palette/density/scale and capped particle counts. Leaf emitters are cached from content metadata instead of tree names. Torch emissions consume the actual local fixture. Oak Forest supplies a local Atmosphere profile; other biomes can reuse these renderers. No UI lives here.


Ambient falling leaves cache the configured generic tree and hit receiver, using TreeDefinition.FoliageColour for the same colour as axe-hit bursts. HasFoliage=false and burning/burnt leaf suppression apply to ambient leaves too. No per-tree leaf colour Resource or per-frame tree search is needed.

P3 source-cache fix: streamed props register/unregister through EnvironmentRuntime.RegisterScenery/UnregisterScenery. Trees keep a dense cached source list with indexed swap removal; registration caches hit receiver, world position and species colour. Startup handles persistent/preview roots only. Chunk completion no longer runs FindChildren over the loaded world. Leaves reject off-view or unpublished sources before drawing; source positions use the renderer's local coordinates. Tree appearance changes refresh one source. Static scenery that moves or changes authored foliage data must explicitly refresh its source registration.
