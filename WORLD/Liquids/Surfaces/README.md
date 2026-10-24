# Liquid surfaces

Implemented pond-style water shaders and WG2 LiquidSurfaceTile fields. The fixed pond retains its original UV sampling; generated tiles sample a world-aligned scalar lattice with halos, preserving one shoreline and wave phase across chunk borders. Each tile owns its field/material/quad and releases them on chunk unload; shader/noise Resources are shared. Depth colour and wet shore reuse canonical definition settings. Detailed banks/carving, other liquid shader families and current flow remain future phases. See [WG2 validation](../WG2_Validation.md).
