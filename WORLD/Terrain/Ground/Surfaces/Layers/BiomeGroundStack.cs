// Explicit required base plus at most two ordered optional overlays; no runtime processing.
using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class BiomeGroundStack : Resource
{
    [ExportGroup("Defining Base Ground - always full coverage")]
    [Export] public BiomeGroundLayer DefiningBaseGround { get; set; }
    [ExportGroup("Organic Overlay Layers - ordered, maximum two")]
    [Export] public Godot.Collections.Array<BiomeGroundLayer> OverlayLayers { get; set; } = new();

    // Validate once when resolving the base, never from a frame callback.
    // =========================================================
    public GroundSurfaceProfile ResolveBaseSurface()
    {
        if (DefiningBaseGround == null || OverlayLayers == null || OverlayLayers.Count > 2)
            throw new ArgumentException("Ground stacks require one base and zero to two overlays.");
        DefiningBaseGround.Validate(true);
        var ids = new HashSet<string>(StringComparer.Ordinal) { DefiningBaseGround.Id };
        foreach (var layer in OverlayLayers)
        {
            if (layer == null) throw new ArgumentException("Ground stack contains a null overlay.");
            layer.Validate(false);
            if (!ids.Add(layer.Id)) throw new ArgumentException("Ground layer IDs must be unique within their stack.");
        }
        return DefiningBaseGround.Surface;
    }
}
