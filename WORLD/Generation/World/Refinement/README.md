# Stateless local macro refinement — BC4

The world retains one bounded low-resolution global cell/region skeleton. `MacroBorderRefinement.Offset` derives a small, continuous two-channel displacement from world-relative coordinates and a versioned integer lattice; it has no sectors, mutable RNG, query cache, Nodes or per-frame controller. Detailed queries refine only the requested point. Macro site indexing is a separate creation-only algorithm.

`MacroWorldMap.RegionAt` and streamed ground lookup use matching CPU/shader formulas. Refinement is at most 0.25 macro cells (shipped 0.2 = 409.6 units) at a two-cell noise scale. Warping is restricted to land→land queries; coarse non-land identity and exact independently frozen shore polygons are not warped. The coarse ownership grid stays four-connected; refinement is presentation/query shaping, not newly generated high-resolution cell arrays. Normal floating-point CPU/GPU edge rounding still needs target visual seam checks.

`MacroSiteIndex` retains at most 16 representatives in each spatial bin and inspects nine bins per candidate, replacing O(all prior sites) with a bounded heuristic for new generator 5. Existing version 1–4 algorithms are untouched when ScalableMacro is false. The site score is not a physical spacing/collision guarantee; existing seeded local scenery rules own that.

Schema 12 stores the refinement strength alongside complete coarse ownership, exact region metadata and seed. Hashes include it for generator 5; restored old generators retain zero refinement and their original hash contract. No floating-origin phase is introduced: [preflight](../WorldMacroBudget.cs) explicitly bounds supported coordinate magnitudes. See [BC4 handoff](../../Authoring/BC4_Validation.md).
