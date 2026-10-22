extends SceneTree
var failures=0
func _initialize():call_deferred("run")
func check(ok,message):
    if not ok:failures+=1;push_error("LOCAL LIGHT FAIL: "+message)
func polygons_match(a,b):
    if a.size()!=b.size():return false
    for i in a.size():
        if a[i].distance_to(b[i])>0.002:return false
    return true
func settle():
    await process_frame
    await process_frame
    if DisplayServer.get_name()!="headless":await RenderingServer.frame_post_draw
func luminance(image,p):
    var total=0.0
    var n=0
    for y in range(-3,4):
        for x in range(-3,4):
            var q=Vector2i(p)+Vector2i(x,y)
            if q.x<0 or q.y<0 or q.x>=image.get_width() or q.y>=image.get_height():continue
            var c=image.get_pixelv(q)
            total+=(c.r+c.g+c.b)/3.0;n+=1
    return total/max(n,1)
func run():
    var world=load("res://SCENES/World/World.tscn").instantiate()
    world.set("Config",world.get("Config").duplicate())
    world.get("Config").set("Streaming",null) # Preserve the authored preview fixture; P3 has its own traversal suite.
    world.get("Config").set("RandomSeedOnLoad",false)
    root.add_child(world)
    var env=world.get_node("SystemsRoot/Environment")
    var profile=env.get("Settings").get("Torch")
    var torch=world.get_node("Actors/Torch")
    check(world.has_node("Actors/PreviewTorch1") and world.has_node("Actors/PreviewTorch2"),"three torches")
    env.get("Clock").get("Config").set("Paused",true)
    env.get("Clock").call("SetHour",0)
    env.get("Wind").get("Config").set("Strength",0)
    var settings=env.get("Settings")
    settings.get("ColourGrade").set("Enabled",false)
    settings.get("Clouds").set("Enabled",false)
    settings.get("Weather").set("RainIntensity",0)
    settings.get("Weather").set("Lightning",false)
    settings.get("Atmosphere").set("Haze",false)
    var nearest=null
    var distance=INF
    var mirrors={}
    for tree in get_nodes_in_group("world_trees"):
        var occluder=tree.get_node("LocalLightOccluder")
        check(polygons_match(occluder.occluder.polygon,tree.get_node("TrunkFootprint").polygon),"mirrored/scaled trunk outline")
        check(not occluder.is_processing(),"no idle occluder process")
        mirrors[tree.get("Mirrored")]=true
        var d=tree.global_position.distance_squared_to(torch.global_position)
        if d<distance:nearest=tree;distance=d
    check(mirrors.size()==2,"both mirror orientations")
    var rocks=0
    for child in world.get_node("Actors").get_children():
        if child.has_node("RockFootprint"):
            rocks+=1
            check(polygons_match(child.get_node("LocalLightOccluder").occluder.polygon,child.get_node("RockFootprint").polygon),"rock blocker")
    check(rocks>0,"rocks present")
    profile.set("Energy",1.2)
    profile.set("TextureScale",1.8)
    profile.set("CastShadows",false)
    await settle()
    check(not torch.get_node("Light").shadow_enabled,"shadow opt-out")
    var range_before=torch.get_node("Light").texture_scale
    profile.set("Energy",0.4)
    await settle()
    check(is_equal_approx(torch.get_node("Light").texture_scale,range_before),"energy independent of range")
    profile.set("CastShadows",true)
    profile.set("TextureScale",1.5)
    await settle()
    for name in ["Torch","PreviewTorch1","PreviewTorch2"]:
        check(world.get_node("Actors/"+name+"/Light").shadow_enabled,"all torches opt-in")
    # Freeze flicker for deterministic image comparisons and isolate one local light.
    for child in env.get_children():
        if str(child.name)=="TorchLight" or str(child.name).begins_with("PreviewTorchLight"):
            child.set_process(false)
            child.get("Torch").get_node("Light").enabled=false
    var light=torch.get_node("Light")
    light.enabled=true;light.energy=1.4;light.texture_scale=2.0
    light.shadow_filter=0
    var player=world.get_node("Actors/Player")
    player.global_position=nearest.global_position+Vector2(0,100)
    player.reset_physics_interpolation()
    await create_timer(0.1).timeout
    if DisplayServer.get_name()!="headless":
        for side in [-1,1]:
            torch.global_position=nearest.global_position+Vector2(side*90,0)
            light.shadow_enabled=false
            await settle()
            var off=root.get_texture().get_image()
            light.shadow_enabled=true
            await settle()
            var on=root.get_texture().get_image()
            var probe=nearest.get_global_transform_with_canvas()*(Vector2(-side*52,23)/nearest.scale)
            probe*=Vector2(on.get_size())/root.get_visible_rect().size
            var delta=luminance(off,probe)-luminance(on,probe)
            print("SHADOW DARKENING ",side,": ",delta)
            check(delta>0.004,"rendered shadow on opposite side of torch")
            on.save_png("user://local-light-shadow-"+str(side)+".png")
            off.save_png("user://local-light-no-shadow-"+str(side)+".png")
    check(env.get_node("SunShadows").get_child_count()>0,"sun shadow cache retained")
    print("LOCAL LIGHT CHECKS: "+str(failures)+" failures")
    quit(1 if failures else 0)
