# WORLD/Content/Vegetation/Trees

**Implemented:** Shared/TreeDefinition.cs and Oak/Oak.tres, scene and sprite. Trees are static; no harvesting or environmental behaviour is promoted yet.

Tree species definitions live here, each in a folder such as `Oak/` with `Oak.tres` and `Sprites/Oak_01.png` ... `Oak_16.png` when created. One tree species can appear in several biomes with different spawn weights. Mirroring and modest scaling are per-species/variant permissions; tree ground anchors and root collider shape must remain accurate.

> Other content/terrain systems remain scaffolding; the implemented foundation is described above.
