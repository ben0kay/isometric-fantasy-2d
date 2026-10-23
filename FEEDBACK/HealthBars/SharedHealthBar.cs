// Event-driven world-space health bar; binds the existing ActorHealth, never owns or changes HP.
using Godot;
using System;

public partial class SharedHealthBar : Node2D
{
    [Export] public HealthBarSettings Settings { get; set; }
    private ActorHealth _health;
    private float _width, _display, _target, _start, _elapsed;

    #region Lifecycle
    // Idle health bars never process frames.
    // =========================================================
    public override void _Ready()
    {
        SetProcess(false);
        Settings ??= GD.Load<HealthBarSettings>("res://FEEDBACK/HealthBars/DefaultHealthBarSettings.tres");
        Visible = false;
    }

    // Bind existing runtime HP after the actor has constructed its health state.
    // =========================================================
    public void Bind(ActorHealth health, Sprite2D artwork)
    {
        Unbind();
        if (health == null || Settings == null) return;
        _health = health;
        _health.Changed += OnChanged;
        if (artwork != null && artwork.Texture != null)
        {
            Rect2 rect = artwork.GetRect();
            Transform2D transform = artwork.Transform;
            Vector2 a = transform * rect.Position;
            Vector2 b = transform * new Vector2(rect.End.X, rect.Position.Y);
            Vector2 c = transform * rect.End;
            Vector2 d = transform * new Vector2(rect.Position.X, rect.End.Y);
            Position = new Vector2((Mathf.Min(Mathf.Min(a.X,b.X),Mathf.Min(c.X,d.X)) +
                Mathf.Max(Mathf.Max(a.X,b.X),Mathf.Max(c.X,d.X))) * 0.5f,
                Mathf.Min(Mathf.Min(a.Y,b.Y),Mathf.Min(c.Y,d.Y)) - Settings.VerticalGap + Settings.VerticalOffset);
        }
        _width = CalculateWidth(health.Maximum);
        _target = _display = health.Current / (float)health.Maximum;
        Visible = ShouldShow();
        QueueRedraw();
    }

    // Dispose event registration when actor/chunk or HUD exits.
    // =========================================================
    public override void _ExitTree() => Unbind();

    // Avoid dangling event references after chunk/player unload.
    // =========================================================
    private void Unbind()
    {
        SetProcess(false);
        if (_health != null) _health.Changed -= OnChanged;
        _health = null;
    }
    #endregion

    #region Presentation
    // Restore/reload updates the bar without animating damage or replaying feedback.
    // =========================================================
    private void OnChanged(HealthChange change)
    {
        if (_health == null) return;
        _target = _health.Current / (float)_health.Maximum;
        Visible = ShouldShow();
        if (!Visible || change.Restored || !Settings.SmoothChanges || Settings.AnimationSeconds <= 0f)
        {
            _display = _target; SetProcess(false);
        }
        else
        {
            _start = _display; _elapsed = 0f;
            SetProcess(Mathf.Abs(_start - _target) > 0.0001f);
        }
        QueueRedraw();
    }

    // Only the brief fill interpolation needs per-frame updates.
    // =========================================================
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float t = Mathf.Clamp(_elapsed / Mathf.Max(0.001f, Settings.AnimationSeconds),0f,1f);
        _display = Mathf.Lerp(_start,_target,t);
        QueueRedraw();
        if (t >= 1f) SetProcess(false);
    }

    // Limit maximum health influence so bosses don't get enormous bars.
    // =========================================================
    private float CalculateWidth(int maxHp)
    {
        float min = Mathf.Max(8f,Settings.MinimumWidth), max = Mathf.Max(min,Settings.MaximumWidth);
        double lo = Math.Log(Math.Max(1,Settings.HealthAtMinimumWidth));
        double hi = Math.Log(Math.Max(Settings.HealthAtMinimumWidth + 1,Settings.HealthAtMaximumWidth));
        float t = (float)Math.Clamp((Math.Log(Math.Max(1,maxHp))-lo)/(hi-lo),0d,1d);
        return Mathf.Lerp(min,max,t);
    }

    // Hide unused, full-health and dead bars by default.
    // =========================================================
    private bool ShouldShow() => _health != null && Settings.Enabled && _health.IsAlive &&
        (Settings.ShowAtFullHealth || _health.Current < _health.Maximum);

    // Draw one lightweight world-space canvas item only on an HP change/short tween.
    // =========================================================
    public override void _Draw()
    {
        if (!Visible || Settings == null) return;
        float h = Mathf.Max(2f,Settings.BarHeight);
        Rect2 track = new(-_width * .5f,-h * .5f,_width,h);
        DrawRect(track,Settings.Background);
        float fillWidth = Mathf.Clamp(_display,0f,1f)*_width;
        Color fill = _target <= Settings.CriticalThreshold ? Settings.Critical :
            _target <= Settings.WarningThreshold ? Settings.Warning : Settings.Healthy;
        if (fillWidth > 0f) DrawRect(new Rect2(track.Position,new Vector2(fillWidth,h)),fill);
        DrawRect(track,Settings.Border,false,1f);
    }
    #endregion
}
