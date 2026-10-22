extends SceneTree
func _initialize(): call_deferred("run")
func anchor(s,p):
    if s.flip_h:p.x=s.texture.get_width()-p.x
    if s.flip_v:p.y=s.texture.get_height()-p.y
    return s.to_global(s.offset+p)
func check(ok,message):
    if not ok:
        push_error("FAIL: "+message)
        quit(2)
        assert(ok,message)
func run():
    var world=load("res://SCENES/World/World.tscn").instantiate()
    world.get("Config").set("RandomSeedOnLoad",false)
    root.add_child(world)
    var burns=get_nodes_in_group("world_burnables")
    check(burns.size()>0,"receivers registered")
    var normal=null
    var mirrored=null
    for b in burns:
        check(not b.is_processing(),"idle receiver")
        if b.get_parent().get_node("Sprite").flip_h:mirrored=b
        else:normal=b
    check(normal!=null and mirrored!=null,"normal and mirrored oaks")
    var runtime=world.get_node("Fire")
    var env=world.get_node("Environment")
    var snow=env.get("Snow")
    var wind=env.get("Wind")
    var pairs=[]
    for b in [normal,mirrored]:
        var t=b.get_parent()
        var s=t.get_node("Sprite")
        var profile=b.get("Profile")
        profile.set("IgnitionDelaySeconds",0.1)
        profile.set("TransitionSeconds",2)
        pairs.append([b,s.texture,anchor(s,profile.get("HealthyAnchor")),t.transform,t.get_node("TrunkFootprint").polygon,s.material])
        check(b.call("TryIgnite",t.global_position,"Validation"),"ignite")
        check(not b.call("TryIgnite",t.global_position,"Repeated"),"no repeated ignition")
        check(not t.get_node("HitResponse").get("HealthyLeaves"),"leaves disabled")
        check(t.has_node("FireEffects"),"production effects")
    var player=world.get_node("Actors/Player")
    player.global_position=normal.get_parent().global_position+Vector2(100,50)
    wind.get("Config").set("Strength",-2)
    await create_timer(0.35).timeout
    check(runtime.get("ActiveEffectCount")==2,"effect count")
    var fx=normal.get_parent().get_node("FireEffects")
    check(fx.get_node("Flames").process_material.direction.x<0,"left wind")
    snow.get("Config").set("Enabled",true)
    snow.get("Config").set("TransitionSeconds",0.1)
    snow.call("SetCoverage",0.8)
    wind.get("Config").set("Strength",2)
    await create_timer(0.35).timeout
    check(fx.get_node("Flames").process_material.direction.x>0,"right wind")
    if DisplayServer.get_name()!="headless":
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("user://production-fire-burning.png")
    snow.get("Config").set("Enabled",false)
    await create_timer(1.6).timeout
    for pair in pairs:
        var b=pair[0]
        var t=b.get_parent()
        var s=t.get_node("Sprite")
        check(b.get("State")==2 and not b.is_processing(),"burn completed")
        check(s.texture==b.get("Profile").get("BurntTexture"),"charred art")
        check(anchor(s,b.get("Profile").get("BurntAnchor")).distance_to(pair[2])<0.01,"trunk aligned")
        check(t.transform==pair[3] and t.get_node("TrunkFootprint").polygon==pair[4],"collider stable")
        check(s.material==pair[5],"wind material restored")
        var shadow=null
        for candidate in env.get_node("SunShadows").get_children():
            if candidate is Sprite2D and candidate.texture==s.texture and candidate.flip_h==s.flip_h:shadow=candidate
        check(shadow!=null,"charred shadow found")
        check(shadow.texture==s.texture and shadow.offset==s.offset,"shadow refreshed")
        b.call("ResetBurn")
        check(s.texture==pair[1] and t.get_node("HitResponse").get("HealthyLeaves"),"reset art and leaves")
        check(shadow.texture==s.texture,"reset shadow")
    await process_frame
    check(runtime.get("ActiveEffectCount")==0,"reset frees effects")
    # Feature works with tooling removed, capped effects do not cap burn states.
    world.get_node("FireDebugPanel").queue_free()
    await process_frame
    runtime.get("Config").set("MaxActiveEffects",1)
    for b in [normal,mirrored]:check(b.call("TryIgnite",b.get_parent().global_position,"NoUI"),"no UI ignition")
    check(runtime.get("ActiveEffectCount")==1,"bounded effects")
    for b in [normal,mirrored]:b.call("ResetBurn")
    await process_frame
    check(runtime.get("ActiveEffectCount")==0,"second reset cleanup")
    if DisplayServer.get_name()!="headless":
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("user://production-fire-reset.png")
    print("PRODUCTION FIRE CHECKS PASSED: normal/mirrored anchors, lifecycle, collider, wind, snow transitions, shadows, reset, optional UI and effect cap")
    quit()
