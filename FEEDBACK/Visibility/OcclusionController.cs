// World-level visual-only occlusion for any registered prop; no per-prop ticking.
using Godot;
using System.Collections.Generic;
using FantasyGame.World.Trees;

public partial class OcclusionController : Node
{
    [Export(PropertyHint.Range, "0.03,0.5,0.01")] public float CheckIntervalSeconds { get; set; } = 0.10f;
    [Export] public Vector2 PlayerVisibilityOffset { get; set; } = new(0f, -16f);

    private Node2D _player;
    private float _untilCheck;
    private readonly Dictionary<Sprite2D, Entry> _registered = new();
    private readonly List<Sprite2D> _invalid = new();

    private sealed class Entry
    {
        public OcclusionProfile Profile;
        public Node2D Owner;
        public float BaseAlpha;
        public float CurrentAlpha;
        public bool Obstructing;
    }

    #region Registration
    // Start only once, after authored/scattered world props are configured.
    // =========================================================
    public void Initialize(Node2D player)
    {
        _player = player;
        foreach (Node node in GetTree().GetNodesInGroup("world_trees"))
            if (node is Tree tree && tree.Definition?.VisibilityProfile is { Enabled: true } profile)
                Register(tree, tree.GetNodeOrNull<Sprite2D>("Sprite"), profile);
        SetProcess(true);
    }

    // Non-tree props may opt in without adding their own fade script.
    // =========================================================
    public void Register(Node2D owner, Sprite2D visual, OcclusionProfile profile)
    {
        if (owner == null || visual == null || profile == null || !profile.Enabled || _registered.ContainsKey(visual)) return;
        float alpha = visual.Modulate.A;
        _registered.Add(visual, new Entry { Owner = owner, Profile = profile, BaseAlpha = alpha, CurrentAlpha = alpha });
    }

    // Restore the exact original visual alpha when unregistering.
    // =========================================================
    public void Unregister(Sprite2D visual)
    {
        if (visual == null || !_registered.Remove(visual, out Entry entry)) return;
        if (GodotObject.IsInstanceValid(visual))
        {
            Color color = visual.Modulate; color.A = entry.BaseAlpha; visual.Modulate = color;
        }
    }
    #endregion

    #region Detection and easing
    // Candidate checks are throttled; opacity transitions remain smooth each frame.
    // =========================================================
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_player)) return;
        float dt = (float)delta;
        _untilCheck -= dt;
        if (_untilCheck <= 0f)
        {
            _untilCheck = Mathf.Max(0.03f, CheckIntervalSeconds);
            Vector2 playerPoint = _player.GlobalPosition + PlayerVisibilityOffset;
            foreach (var pair in _registered)
            {
                Sprite2D visual = pair.Key;
                Entry entry = pair.Value;
                if (!GodotObject.IsInstanceValid(visual) || !GodotObject.IsInstanceValid(entry.Owner))
                {
                    _invalid.Add(visual);
                    continue;
                }
                entry.Obstructing = IsObstructing(visual, entry.Owner, playerPoint, _player.GlobalPosition, entry.Profile);
            }
        }
        foreach (var pair in _registered)
        {
            Sprite2D sprite = pair.Key;
            Entry entry = pair.Value;
            if (!GodotObject.IsInstanceValid(sprite)) continue;
            float target = entry.Obstructing ? entry.BaseAlpha * Mathf.Clamp(entry.Profile.MinimumAlpha, 0.05f, 1f) : entry.BaseAlpha;
            if (Mathf.IsEqualApprox(entry.CurrentAlpha, target)) continue;
            float duration = target < entry.CurrentAlpha ? entry.Profile.FadeOutSeconds : entry.Profile.FadeInSeconds;
            entry.CurrentAlpha = Mathf.MoveToward(entry.CurrentAlpha, target, dt / Mathf.Max(0.05f, duration) * entry.BaseAlpha);
            Color tint = sprite.Modulate;
            tint.A = entry.CurrentAlpha;
            sprite.Modulate = tint;
        }
        foreach (Sprite2D sprite in _invalid) _registered.Remove(sprite);
        _invalid.Clear();
    }

    // Check actual transformed sprite corners, not tree species or collision shapes.
    // =========================================================
    private static bool IsObstructing(Sprite2D sprite, Node2D owner, Vector2 point, Vector2 playerFeet, OcclusionProfile profile)
    {
        if (!profile.Enabled || !sprite.Visible || sprite.Texture == null) return false;
        if (playerFeet.Y > owner.GlobalPosition.Y + profile.BehindAnchorTolerance) return false;
        // Cheap conservative world radius before testing transformed sprite corners.
        float radius = sprite.Texture.GetSize().Length() * sprite.GlobalScale.Length() * 0.7f + profile.VisualPadding;
        if (point.DistanceSquaredTo(owner.GlobalPosition) > radius * radius) return false;
        Rect2 r = sprite.GetRect();
        Vector2 a = sprite.ToGlobal(r.Position), b = sprite.ToGlobal(r.Position + new Vector2(r.Size.X, 0f));
        Vector2 c = sprite.ToGlobal(r.Position + new Vector2(0f, r.Size.Y)), d = sprite.ToGlobal(r.End);
        float minX = Mathf.Min(Mathf.Min(a.X, b.X), Mathf.Min(c.X, d.X)) - profile.VisualPadding;
        float maxX = Mathf.Max(Mathf.Max(a.X, b.X), Mathf.Max(c.X, d.X)) + profile.VisualPadding;
        float minY = Mathf.Min(Mathf.Min(a.Y, b.Y), Mathf.Min(c.Y, d.Y)) - profile.VisualPadding;
        float maxY = Mathf.Max(Mathf.Max(a.Y, b.Y), Mathf.Max(c.Y, d.Y)) + profile.VisualPadding;
        return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
    }
    #endregion

    // Avoid leaving faded objects behind when world controller exits.
    // =========================================================
    public override void _ExitTree()
    {
        foreach (var pair in _registered)
            if (GodotObject.IsInstanceValid(pair.Key))
            {
                Color tint = pair.Key.Modulate; tint.A = pair.Value.BaseAlpha; pair.Key.Modulate = tint;
            }
        _registered.Clear();
    }
}
