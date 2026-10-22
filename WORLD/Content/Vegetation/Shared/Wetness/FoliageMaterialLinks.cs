// World-owned links forward changing wetness to composed overlays without idle polling.
using Godot;
using System.Collections.Generic;
public sealed class FoliageMaterialLinks
{
    private readonly Dictionary<ShaderMaterial, List<ShaderMaterial>> _links = new();
    // Register once when an overlay is built; initialize it from its source.
    // =========================================================
    public void Register(ShaderMaterial source, ShaderMaterial overlay)
    {
        if (!_links.TryGetValue(source, out var targets)) _links.Add(source, targets = new());
        targets.Add(overlay); Copy(source, overlay);
    }
    public void Unregister(ShaderMaterial source, ShaderMaterial overlay)
    {
        if (!_links.TryGetValue(source, out var targets)) return;
        targets.Remove(overlay); if (targets.Count == 0) _links.Remove(source);
    }
    // Only a changed wet material visits its registered overlays.
    // =========================================================
    public void Update(ShaderMaterial source)
    {
        if (_links.TryGetValue(source, out var targets)) foreach (var target in targets) Copy(source, target);
    }
    private static void Copy(ShaderMaterial source, ShaderMaterial target)
    {
        target.SetShaderParameter("wet_coverage", source.GetShaderParameter("wet_coverage"));
        target.SetShaderParameter("wet_darkening", source.GetShaderParameter("wet_darkening"));
    }
}
