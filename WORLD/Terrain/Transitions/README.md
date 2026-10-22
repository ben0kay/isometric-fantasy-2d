# Terrain transitions
AdjacentBiomeBlend.gdshaderinc shares the analytical curved boundary used by AdjacentBiomeLayout. The ground renderer receives the geometry and both surface profiles once, then blends colours and variation/detail settings in world space. Sampling and placement use the same curve, avoiding a visual/physical mismatch.

The current two grass-family surfaces share artwork, shader composition and noise; each has its own editable palette and detail/variation data. Arbitrary material/texture transitions and a many-biome generator remain future work. No shader copies per biome or nodes per tile are added.
