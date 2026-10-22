# Liquid Regions

**Scaffolding only.** The future `LiquidRegion` component defines each bounded liquid body independently of liquid type. One authoritative polygon or equivalent shape supplies signed shoreline distance, `Contains`, rendering mask, scenery exclusion, depth/wading and impact sampling. Allow a pre-authored or future generated shoreline; don't always generate a fixed pond ellipse. Cache polygon edges and distance-field rendering textures when shape changes, not per frame. DEBUG prototype: `DEBUG/TopDownTest/Liquids/LiquidRegion.cs`.
