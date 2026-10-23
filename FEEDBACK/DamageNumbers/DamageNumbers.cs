// One reusable floating text label per actor; listens to actual HP loss, without damage logic.
using Godot;
using System;

public partial class DamageNumbers : Node2D
{
    [Export] public DamageNumberSettings Settings { get; set; }
    private ActorHealth _health;
    private Label _label;
    private Vector2 _anchor;
    private float _elapsed;
    private float _sinceLastDamage;
    private int _amount;

    #region Setup
    // Create a single label and disable all idle animation work.
    // =========================================================
    public override void _Ready()
    {
        SetProcess(false);
        Settings ??= GD.Load<DamageNumberSettings>("res://FEEDBACK/DamageNumbers/DefaultDamageNumberSettings.tres");
        _label = new Label { Name = "Value", MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center, Visible = false };
        AddChild(_label);
        if (Settings != null)
        {
            _label.AddThemeFontSizeOverride("font_size", Settings.FontSize);
            _label.AddThemeColorOverride("font_color", Settings.TextColor);
            _label.AddThemeColorOverride("font_outline_color", Settings.OutlineColor);
            _label.AddThemeConstantOverride("outline_size", Settings.OutlineSize);
        }
    }

    // Follow the top of actual rendered sprite art; avoid global position tracking.
    // =========================================================
    public void Bind(ActorHealth health, Sprite2D artwork)
    {
        Unbind();
        if (health == null || Settings == null || !Settings.Enabled) return;
        _health = health;
        if (artwork?.Texture != null)
        {
            Rect2 rect = artwork.GetRect();
            Transform2D xf = artwork.Transform;
            Vector2 a = xf * rect.Position, b = xf * new Vector2(rect.End.X,rect.Position.Y);
            Vector2 c = xf * rect.End, d = xf * new Vector2(rect.Position.X,rect.End.Y);
            _anchor = new Vector2((Mathf.Min(Mathf.Min(a.X,b.X),Mathf.Min(c.X,d.X)) +
                Mathf.Max(Mathf.Max(a.X,b.X),Mathf.Max(c.X,d.X)))*0.5f,
                Mathf.Min(Mathf.Min(a.Y,b.Y),Mathf.Min(c.Y,d.Y)) - Settings.VerticalGap + Settings.VerticalOffset);
        }
        else _anchor = new Vector2(0,-32-Settings.VerticalGap+Settings.VerticalOffset);
        Position = _anchor;
        _health.Changed += OnHealthChanged;
        HideNumber();
    }

    // Never retain an ActorHealth event subscription after actor/scene unload.
    // =========================================================
    public override void _ExitTree() => Unbind();

    // =========================================================
    private void Unbind()
    {
        if (_health != null) _health.Changed -= OnHealthChanged;
        _health = null;
        HideNumber();
    }
    #endregion

    #region Damage presentation
    // Only display real HP loss; restoration, heal, block and zero damage are silent.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    {
        if (change.Restored || change.After >= change.Before || Settings?.Enabled != true) return;
        int applied = change.Before - change.After;
        bool merge = _label.Visible && _sinceLastDamage <= Settings.MergeWindowSeconds;
        _amount = merge ? Math.Min(1000000, _amount + applied) : applied;
        _label.Text = "-" + _amount;
        // Center using label's own measured minimum size, not a per-frame font query.
        _label.ResetSize();
        _label.Size = _label.GetCombinedMinimumSize();
        _label.PivotOffset = _label.Size * 0.5f;
        _label.Position = new Vector2(-_label.Size.X*0.5f,-_label.Size.Y*0.5f);
        _label.Visible = true;
        _elapsed = 0f;
        _sinceLastDamage = 0f;
        _label.Modulate = Colors.White;
        _label.Scale = Vector2.One * Settings.PopScale;
        SetProcess(true);
    }

    // Run only while text floats and fades, then stop completely.
    // =========================================================
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        _sinceLastDamage += (float)delta;
        float t = Mathf.Clamp(_elapsed / Mathf.Max(0.01f,Settings.DurationSeconds),0f,1f);
        Position = _anchor + Vector2.Up * (Settings.RiseDistance * t);
        _label.Scale = Vector2.One * Mathf.Lerp(Settings.PopScale,1f,t);
        _label.Modulate = new Color(1f,1f,1f,1f-t);
        if (t >= 1f) HideNumber();
    }

    // =========================================================
    private void HideNumber()
    {
        SetProcess(false);
        if (GodotObject.IsInstanceValid(_label)) _label.Visible = false;
        _amount = 0;
        _elapsed = 0f; _sinceLastDamage = 0f;
    }
    #endregion
}
