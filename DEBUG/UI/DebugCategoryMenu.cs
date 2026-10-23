// Extensible exclusive category drawer; selecting a category never disables its simulation.
using Godot;
using System;
using System.Collections.Generic;
public partial class DebugCategoryMenu : PanelContainer
{
    private readonly Dictionary<string,(Button Button,VBoxContainer Body)> _categories=new();
    private readonly ButtonGroup _group=new() { AllowUnpress=true };
    private VBoxContainer _buttons, _contents;
    public string ActiveCategory { get; private set; } = "";
    public event Action<string> CategoryChanged;
    // A scrollable body shares one shell; category buttons stay pinned at the top left.
    // =========================================================
    public override void _Ready()
    {
        var layout=new VBoxContainer(); AddChild(layout);
        layout.AddChild(new Label { Text="Debug controls — F9" });
        _buttons=new VBoxContainer(); layout.AddChild(_buttons);
        var scroll=new ScrollContainer { SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled };
        layout.AddChild(scroll); _contents=new VBoxContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill }; scroll.AddChild(_contents);
        Resized+=ResizeBody; ResizeBody();
    }
    // Future categories need only a name and body; ButtonGroup guarantees exclusive selection.
    // =========================================================
    public VBoxContainer AddCategory(string name)
    {
        var button=new Button { Text=name,ToggleMode=true,ButtonGroup=_group,FocusMode=Control.FocusModeEnum.None };
        var body=new VBoxContainer { Name=name+"Controls",Visible=false,SizeFlagsHorizontal=SizeFlags.ExpandFill };
        _buttons.AddChild(button); _contents.AddChild(body); _categories.Add(name,(button,body));
        button.Toggled+=pressed=> { body.Visible=pressed; if(pressed) ActiveCategory=name; else if(ActiveCategory==name) ActiveCategory=""; CategoryChanged?.Invoke(ActiveCategory); };
        return body;
    }
    // Explicit selection is also useful for focused UI regression checks.
    // =========================================================
    public void SelectCategory(string name) { if(_categories.TryGetValue(name,out var entry)) entry.Button.ButtonPressed=true; }
    // =========================================================
    private void ResizeBody() { if(_contents!=null) _contents.CustomMinimumSize=new Vector2(Mathf.Max(120,Size.X-28),0); }
    // =========================================================
    public override void _ExitTree() => _group.Dispose();
}
