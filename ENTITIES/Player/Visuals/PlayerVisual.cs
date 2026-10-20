// Aligns the upright sprite once to the fixed camera, independently of movement.
using Godot;

public partial class PlayerVisual : Node3D
{
    [Export] public PlayerMasterConfig Config { get; set; }

    // Defer until the scene's camera has initialized; the body never needs to face it.
    // =========================================================
    public override void _Ready() => Callable.From(ConfigureSprite).CallDeferred();

    // Keep world-up vertical and place the texture's bottom edge at ground contact.
    // =========================================================
    private void ConfigureSprite()
    {
        Sprite3D sprite = GetNode<Sprite3D>("Sprite");
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera != null) Rotation = new Vector3(0, camera.GlobalRotation.Y, 0);
        sprite.PixelSize = Config?.SpritePixelSize ?? 0.025f;
        sprite.Position = new Vector3(0, sprite.Texture.GetHeight() * sprite.PixelSize * 0.5f, 0);
    }
}
