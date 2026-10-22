// Explicit scene routing: menu UI never manipulates World.tscn internal nodes.
using Godot;
public static class MenuNavigation
{
    public const string Boot = "res://UI/Menus/Boot/BootScreen.tscn";
    public const string Main = "res://UI/Menus/Main/MainMenu.tscn";
    public const string World = "res://SCENES/World/World.tscn";
    // =========================================================
    public static void Open(Node sender, string path)
    {
        SceneTree tree = sender.GetTree();
        tree.Paused = false;
        Error error = tree.ChangeSceneToFile(path);
        if (error != Error.Ok) GD.PushError("Menu navigation failed: " + path + " (" + error + ")");
    }
}
