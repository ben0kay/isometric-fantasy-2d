// First-run boot: select/create a local profile before opening the fantasy main menu.
using Godot;
using System;
public partial class BootScreen : Control
{
    private VBoxContainer _profiles;
    private Label _status;
    private LineEdit _name;
    // =========================================================
    public override void _Ready()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        ProfileStore.Load();
        MenuScreenStyle.AddBackground(this);
        VBoxContainer panel = MenuScreenStyle.CreatePanel(this, "CHOOSE YOUR PATH", "Select a traveller or begin anew.");
        _profiles = new VBoxContainer(); panel.AddChild(_profiles);
        panel.AddChild(new HSeparator());
        _name = new LineEdit { PlaceholderText = "New profile name (2–32 characters)", MaxLength = 32 };
        panel.AddChild(_name);
        panel.AddChild(UIButtonFactory.Create("CREATE PROFILE", CreateProfile));
        panel.AddChild(UIButtonFactory.Create("EXIT", () => GetTree().Quit(), UIButtonFactory.Variant.Secondary));
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        panel.AddChild(_status);
        Refresh();
    }
    // =========================================================
    private void Refresh()
    {
        foreach (Node child in _profiles.GetChildren()) { _profiles.RemoveChild(child); child.QueueFree(); }
        if (ProfileStore.Profiles.Count == 0) _profiles.AddChild(new Label { Text = "No travellers yet." });
        foreach (var profile in ProfileStore.Profiles)
        {
            string id = profile.Id;
            _profiles.AddChild(UIButtonFactory.Create(profile.Name, () =>
            {
                if (ProfileStore.Select(id)) MenuNavigation.Open(this, MenuNavigation.Main);
            }, UIButtonFactory.Variant.Secondary));
        }
        _status.Text = ProfileStore.LastError;
    }
    // =========================================================
    private void CreateProfile()
    {
        try { ProfileStore.Create(_name.Text); MenuNavigation.Open(this, MenuNavigation.Main); }
        catch (Exception error) { _status.Text = error.Message; }
    }
}
