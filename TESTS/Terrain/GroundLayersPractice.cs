// Disposable GS3/GS4 comparison scene; key/widgets update shared uniforms only on input.
using Godot;
public partial class GroundLayersPractice : Node2D
{
    private WorldBiomeDefinition[] _biomes;
    private GroundSurfacePreview _preview;
    private Camera2D _camera;
    private DebugCategoryMenu _menu;
    // Fixed map and sixteen 512-unit chunks expose seams and negative-coordinate sampling.
    // =========================================================
    public override void _Ready()
    {
        _biomes=GroundLayerFixture.Definitions();
        _camera=new Camera2D { Zoom=new Vector2(0.45f,0.45f) }; AddChild(_camera);
        var material=StreamedGroundPalette.Build(GroundLayerFixture.Map(),_biomes);
        _preview=new GroundSurfacePreview(material,true);
        for(int y=0;y<4;y++) for(int x=0;x<4;x++)
            AddChild(StreamedGroundPalette.Create(new Rect2(-1024+x*512,-1024+y*512,512,512),material));
        var ui=new CanvasLayer(); AddChild(ui);
        ui.AddChild(new Label { Position=new Vector2(390,16),Text="Sand left / Soil right | F9 Ground controls | 1/2 toggle layers | R Reset | Z Zoom" });
        _menu=new DebugCategoryMenu { OffsetLeft=16,OffsetTop=16,OffsetRight=366,Visible=false }; ui.AddChild(_menu);
        var body=_menu.AddCategory("Ground Surfaces");
        body.AddChild(new GroundSurfaceDebugPanel { PreviewSource=_preview,BiomeSource=()=>_biomes[0] });
        _menu.SelectCategory("Ground Surfaces");
        GetViewport().SizeChanged+=ResizeControls; ResizeControls(); SetProcess(false);
    }
    // Same preview object drives keyboard and F9, so toggles never rebuild maps or materials.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if(input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if(key.Keycode==Key.Key1) _preview.SetShowOverlay(0,!_preview.ShowOverlay(0));
        else if(key.Keycode==Key.Key2) _preview.SetShowOverlay(1,!_preview.ShowOverlay(1));
        else if(key.Keycode==Key.R) _preview.Reset();
        else if(key.Keycode==Key.F9) _menu.Visible=!_menu.Visible;
        else if(key.Keycode==Key.Z) _camera.Zoom=_camera.Zoom.X<0.5f ? Vector2.One : new Vector2(0.45f,0.45f);
        else return;
        GetViewport().SetInputAsHandled();
    }
    // Fit controls to the window without layout polling.
    // =========================================================
    private void ResizeControls()
    { _menu.OffsetBottom=Mathf.Max(180,GetViewport().GetVisibleRect().Size.Y-16); _menu.OffsetRight=Mathf.Min(366,GetViewport().GetVisibleRect().Size.X-16); }
    // The external viewport outlives this disposable scene.
    // =========================================================
    public override void _ExitTree() { if(_menu!=null) GetViewport().SizeChanged-=ResizeControls; }
}
