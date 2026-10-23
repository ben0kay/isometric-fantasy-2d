// Small runtime-setting widgets shared by developer panels; no independent update callbacks.
using Godot;
using System;
using System.Collections.Generic;
public sealed class DebugWidgets
{
    private readonly List<Action> _refresh=new();
    private bool _syncing;
    // =========================================================
    public void Toggle(VBoxContainer parent,string title,Func<bool> get,Action<bool> set)
    {
        var control=new CheckButton { Text=title,ButtonPressed=get(),FocusMode=Control.FocusModeEnum.None }; parent.AddChild(control);
        control.Toggled+=value=> { if(!_syncing) set(value); }; _refresh.Add(()=>control.SetPressedNoSignal(get()));
    }
    // =========================================================
    public void Slider(VBoxContainer parent,string title,float min,float max,float step,Func<float> get,Action<float> set)
    {
        var label=new Label(); var control=new HSlider { MinValue=min,MaxValue=max,Step=step,Value=get(),FocusMode=Control.FocusModeEnum.None };
        parent.AddChild(label); parent.AddChild(control); control.ValueChanged+=value=> { if(!_syncing) set((float)value); };
        _refresh.Add(()=> { control.SetValueNoSignal(get()); label.Text=$"{title}: {get():0.00}"; });
    }
    // =========================================================
    public void Refresh() { _syncing=true; foreach(var update in _refresh) update(); _syncing=false; }
}
