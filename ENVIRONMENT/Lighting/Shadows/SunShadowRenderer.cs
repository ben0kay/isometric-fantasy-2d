// Cached projected sun silhouettes copied from the debug renderer, independent of clock and controls.
using Godot;
using System.Collections.Generic;
public partial class SunShadowRenderer : Node2D
{
    private sealed class Caster { public Node2D Body; public Sprite2D Source, Shadow; public Polygon2D Contact; }
    private readonly List<Caster> _casters = new();
    private readonly List<ShaderMaterial> _windMaterials = new();
    private readonly HashSet<ShaderMaterial> _shadowMaterials = new();
    public WindController Wind { get; set; }
    // Capture the fully generated production actors once; grass opts out through content metadata.
    // =========================================================
    public void Attach(Node2D actors)
    {
        Node2D shadows = this;
        Shader flatShader = GD.Load<Shader>("res://ENVIRONMENT/Lighting/Shadows/Silhouette.gdshader");
        var flatMaterial = new ShaderMaterial { Shader = flatShader };
        var sharedWindShadows = new Dictionary<Material, ShaderMaterial>();
        foreach (Node child in actors.GetChildren())
        {
            if (child is not Node2D body || (body.HasMeta("environment_shadows") && !body.GetMeta("environment_shadows").AsBool())) continue;
            Sprite2D source = body.GetNodeOrNull<Sprite2D>("Sprite");
            if (source == null) continue;
            Material shadowMaterial = flatMaterial;
            if (source.Material is ShaderMaterial original)
            {
                ShaderMaterial litWind = original;
                if (!sharedWindShadows.TryGetValue(original, out ShaderMaterial windShadow))
                {
                    windShadow = (ShaderMaterial)litWind.Duplicate();
                    windShadow.SetShaderParameter("shadow_only", true);
                    sharedWindShadows.Add(original, windShadow);
                    _windMaterials.Add(windShadow);
                }
                shadowMaterial = windShadow;
            }
            if (shadowMaterial is ShaderMaterial cachedShadow) _shadowMaterials.Add(cachedShadow);
            var shadow = new Sprite2D
            {
                Name = body.Name + "SunShadow", Texture = source.Texture,
                Centered = source.Centered, Offset = source.Offset,
                FlipH = source.FlipH, FlipV = source.FlipV,
                TextureFilter = source.TextureFilter, Material = shadowMaterial
            };
            shadows.AddChild(shadow);

            var caster = new Caster { Body = body, Source = source, Shadow = shadow }; _casters.Add(caster);
            // The player already has a contact shadow; trees and rocks get matching grounding.
            if (body.GetNodeOrNull<Polygon2D>("Shadow") == null)
                caster.Contact = AddContactShadow(body, shadows);
        }
    }
    // Refresh one changed sprite without rebuilding shared shadow materials or unrelated casters.
    // =========================================================
    public void RefreshAppearance(Node2D body)
    {
        foreach (Node child in GetChildren()) if (child is SunShadowRenderer chunk) chunk.RefreshAppearance(body);
        foreach (Caster caster in _casters)
        {
            if (caster.Body != body) continue;
            caster.Shadow.Texture = caster.Source.Texture; caster.Shadow.Offset = caster.Source.Offset;
            caster.Shadow.Centered = caster.Source.Centered;
            caster.Shadow.FlipH = caster.Source.FlipH; caster.Shadow.FlipV = caster.Source.FlipV;
            return;
        }
    }
    // Add a low-cost, stationary oval under a static tree or rock.
    // =========================================================
    private Polygon2D AddContactShadow(Node2D body, Node2D shadows)
    {
        var points = new Vector2[16];
        // Root scaling is used for variations, so also scale the generated contact oval.
        Vector2 radius = body.HasMeta("environment_contact") ? body.GetMeta("environment_contact").AsVector2() * body.Scale.Abs() : Vector2.Zero;
        if (radius == Vector2.Zero) return null;
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length;
            points[i] = new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
        var contact = new Polygon2D
        {
            Name = body.Name + "Contact", Position = body.Position + new Vector2(0, -3),
            Polygon = points, Color = new Color(0.055f, 0.075f, 0.06f, 0.23f)
        };
        shadows.AddChild(contact); return contact;
    }

    // Use the original flat-ground projection and preserve actual root/sprite transforms.
    // =========================================================
    public void UpdateProjection(float angle, float length, float opacity, float softness)
    {
        foreach (Node child in GetChildren()) if (child is SunShadowRenderer chunk) chunk.UpdateProjection(angle,length,opacity,softness);
        Vector2 direction = Vector2.FromAngle(angle);
        var projection = new Transform2D(Vector2.Right, -direction * Mathf.Max(0.01f, length), Vector2.Zero);
        for (int i = _casters.Count-1; i >= 0; i--)
        {
            var caster = _casters[i];
            if (!GodotObject.IsInstanceValid(caster.Body) || !GodotObject.IsInstanceValid(caster.Source))
            {
                caster.Shadow.QueueFree(); if (GodotObject.IsInstanceValid(caster.Contact)) caster.Contact.QueueFree();
                _casters.RemoveAt(i); continue;
            }
            // Preserve the static body's local scale/rotation so size-varied trees and rocks
            // project silhouettes matching their actual sprite and collision footprints.
            Transform2D bodyBasis = caster.Body.Transform;
            bodyBasis.Origin = Vector2.Zero;
            caster.Shadow.Transform = new Transform2D(0f, caster.Body.Position) * projection * bodyBasis * caster.Source.Transform;
            caster.Shadow.Modulate = new Color(1, 1, 1, opacity);
            caster.Shadow.Visible = opacity > 0.001f && caster.Source.IsVisibleInTree();
        }
        foreach (ShaderMaterial material in _windMaterials)
            material.SetShaderParameter("wind_multiplier", Wind.Strength);
        foreach (ShaderMaterial material in _shadowMaterials) material.SetShaderParameter("softness_uv", softness);
    }
}
