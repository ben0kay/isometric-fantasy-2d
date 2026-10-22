// Lightweight fantasy main menu: profile-aware campaign launcher, P4 saves not yet supported.
using Godot;
public partial class MainMenu : Control
{
    // =========================================================
    public override void _Ready()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (ProfileStore.Selected == null)
        {
            CallDeferred(MethodName.ReturnToBoot);
            return;
        }
        MenuScreenStyle.AddBackground(this);
        VBoxContainer menu = MenuScreenStyle.CreatePanel(this, "THE UNTAMED LANDS", "SURVIVE  ·  EXPLORE  ·  ENDURE");
        Label traveller = new() { Text = "Traveller: " + ProfileStore.Selected.Name, HorizontalAlignment = HorizontalAlignment.Center };
        menu.AddChild(traveller);
        Button continueButton = UIButtonFactory.Create("CONTINUE  (P4 SAVE SYSTEM PENDING)", null, UIButtonFactory.Variant.Secondary);
        continueButton.Disabled = true; menu.AddChild(continueButton);
        menu.AddChild(UIButtonFactory.Create("NEW CAMPAIGN", NewCampaign));
        menu.AddChild(UIButtonFactory.Create("CHANGE PROFILE", () => MenuNavigation.Open(this, MenuNavigation.Boot), UIButtonFactory.Variant.Secondary));
        menu.AddChild(UIButtonFactory.Create("OPTIONS  (COMING LATER)", null, UIButtonFactory.Variant.Secondary));
        menu.GetChild<Button>(menu.GetChildCount() - 1).Disabled = true;
        menu.AddChild(UIButtonFactory.Create("EXIT GAME", () => GetTree().Quit(), UIButtonFactory.Variant.Danger));
    }
    // =========================================================
    private void ReturnToBoot() => MenuNavigation.Open(this, MenuNavigation.Boot);
    // =========================================================
    private void NewCampaign() => MenuNavigation.Open(this, MenuNavigation.World);
}
