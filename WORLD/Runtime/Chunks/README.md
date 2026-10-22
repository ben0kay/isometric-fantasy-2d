# Surface chunks

P3 implements floor-based integer ChunkCoord, including negative world positions, explicit lifecycle states and root-based scenery ownership. See ../Streaming/README.md for the scheduler, limits, session deltas and validation. World regions and streaming chunks are independent grids; a biome can cross many chunks.

The runtime stream owns its dictionaries under one WorldId/surface layer. Generation reads neighbour candidates even if their chunks are unloaded, preventing load-order seams/overlaps. There are no persistent chunk files yet.
