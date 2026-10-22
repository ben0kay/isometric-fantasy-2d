extends SceneTree
const TEMPLATE="res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn"
var failures=0
func _initialize():call_deferred("run")
func check(ok,message):
    if ok:return
    failures+=1
    push_error("GENERIC TREE FAIL: "+message)
func trees_in(world):
    var result=[]
    for tree in get_nodes_in_group("world_trees"):
        if world.is_ancestor_of(tree):result.append(tree)
    return result
func signature(trees):
    var result=[]
    for tree in trees:
        result.append([tree.get_meta("world_content_id"),tree.get_meta("world_tree_variant"),tree.position,tree.scale,tree.get("Mirrored"),tree.get_node("TrunkFootprint").polygon])
    return result
func make_world(override_species=null):
    var world=load("res://SCENES/World/World.tscn").instantiate()
    world.set("Config",world.get("Config").duplicate())
    world.get("Config").set("Streaming",null) # Preserve the authored preview fixture; P3 has its own traversal suite.
    if override_species!=null:
        world.set("Config",world.get("Config").duplicate(true))
        world.get("Config").get("Biomes").get("Primary").get("Populations")[0].set("Content",override_species)
    world.get("Config").set("RandomSeedOnLoad",false)
    root.add_child(world)
    world.get_node("SystemsRoot/Environment").get("Weather").get("Config").set("Lightning",false)
    return world
func anchor(sprite,point):
    var offset=-sprite.texture.get_size()*0.5 if sprite.centered else Vector2.ZERO
    if sprite.flip_h:point.x=sprite.texture.get_width()-point.x
    return sprite.to_global(sprite.offset+point+offset)
func run():
    var world=make_world()
    var trees=trees_in(world)
    check(trees.size()==116,"both existing biome tree counts preserved")
    var initial=signature(trees)
    var samples={}
    for tree in trees:
        var definition=tree.get("Definition")
        var variant=tree.get("SelectedVariant")
        var sprite=tree.get_node("Sprite")
        check(tree.scene_file_path==TEMPLATE,"only generic scene instanced")
        check(definition.get("Variants").is_empty(),"no species PackedScene data")
        check(not tree.is_processing() and not tree.get_node("HitResponse").is_processing() and not tree.get_node("Burnable").is_processing(),"no idle tree processors")
        check(anchor(sprite,variant.get("SourceAnchor")).distance_to(tree.to_global(variant.get("GroundOffset")))<0.001,"ground anchor including mirror")
        var polygon=variant.get("TrunkPolygon").duplicate()
        if tree.get("Mirrored"):
            polygon.reverse()
            for i in polygon.size():polygon[i].x=-polygon[i].x
        check(polygon==tree.get_node("TrunkFootprint").polygon,"selected footprint mirrored")
        check(sprite.get_meta("snow_foliage_profile")==definition.get("SnowFoliageProfile"),"species snow profile")
        if not samples.has(definition.get("Id")):samples[definition.get("Id")]=tree
    check(samples.size()==2,"two configured species")
    for tree in samples.values():
        check(tree.get_node("HitResponse").get("FoliageColour")==tree.get("Definition").get("FoliageColour"),"hit colour comes from tree foliage data")
    var query=load("res://ENTITIES/Player/Interaction/ToolHitQuery.cs").new()
    world.add_child(query)
    var originals=[]
    for tree in samples.values():
        var hit=tree.get_node("HitResponse")
        check(hit.is_in_group("world_tool_targets"),"all species tool registration")
        var shape=tree.get_node("TrunkFootprint").polygon
        originals.append([tree,tree.transform,shape,tree.get_node("Sprite").transform])
        var count=hit.get("ReceivedHits")
        query.call("Strike",tree.global_position+Vector2(55,0),Vector2.LEFT,60,55)
        check(hit.get("ReceivedHits")==count+1,"actual shared tool query hits species")
        check(hit.get("Config").get("LeavesPerHit")>0,"species foliage enabled")
    check(samples["tree.oak"].get_node("HitResponse").get("FoliageColour")!=samples["tree.pink_blossom"].get_node("HitResponse").get("FoliageColour"),"leaves and petals use different data")
    await create_timer(0.12).timeout
    for item in originals:
        check(item[0].get_node("Sprite").transform!=item[3],"sprite wobbles")
        check(item[0].transform==item[1] and item[0].get_node("TrunkFootprint").polygon==item[2],"hit leaves physics stable")
    await create_timer(4).timeout
    for item in originals:check(not item[0].get_node("HitResponse").is_processing(),"hit processing finishes")
    var environment=world.get_node("SystemsRoot/Environment")
    var snow=environment.get("Snow")
    snow.get("Config").set("Enabled",true)
    snow.get("Config").set("TransitionSeconds",0.1)
    snow.call("SetCoverage",0.8)
    await create_timer(0.2).timeout
    for tree in samples.values():
        check(tree.get_node("Sprite").material.shader.code.contains("VegetationSnow.gdshaderinc"),"both species snow adaptation")
        check(tree.get_node("Sprite").material.get_shader_parameter("snow_coverage")>0.5,"snow applied")
    for tree in samples.values():
        var burn=tree.get_node("Burnable")
        burn.get("Profile").set("IgnitionDelaySeconds",0.1)
        burn.get("Profile").set("TransitionSeconds",1.5)
        check(burn.call("TryIgnite",tree.global_position,"GenericTreeTest"),"all species ignite")
        check(not tree.get_node("HitResponse").get("HealthyLeaves"),"burn suppresses healthy leaves even without charred art")
        check(tree.has_node("FireEffects"),"all species get shared GPU flames")
        check(tree.get_node("FireEffects/Flames").process_material.emission_point_count>0,"canopy mask for each species")
    var pink=samples["tree.pink_blossom"]
    var oak=samples["tree.oak"]
    var pink_texture=pink.get_node("Sprite").texture
    var pink_material=pink.get_node("Sprite").material
    check(pink.get_node("Burnable").get("Profile").get("BurntTexture")==null,"no fake blossom charred art")
    environment.get("Wind").get("Config").set("Strength",-2)
    await create_timer(0.2).timeout
    for tree in samples.values():
        check(tree.get_node("FireEffects/Flames").process_material.direction.x<0,"shared wind in flame effects")
        check(tree.get_node("Sprite").material.get_shader_parameter("wind_multiplier")==-2,"shared wind in snow/erosion material")
    world.get_node("WorldActorsRoot/PersistentEntitiesRoot/Player").global_position=pink.global_position+Vector2(100,50)
    world.get_node("WorldActorsRoot/PersistentEntitiesRoot/Player").reset_physics_interpolation()
    await create_timer(0.15).timeout
    if DisplayServer.get_name()!="headless":
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("user://generic-tree-blossom-burning.png")
    check(pink.get_node("Sprite").material==pink_material,"no erosion material when no replacement art")
    await create_timer(1.6).timeout
    for tree in samples.values():check(tree.get_node("Burnable").get("State")==2,"both burns complete")
    check(pink.get_node("Sprite").texture==pink_texture,"blossom keeps original sprite")
    check(oak.get_node("Sprite").texture==oak.get_node("Burnable").get("Profile").get("BurntTexture"),"oak charred art retained")
    for tree in samples.values():tree.get_node("Burnable").call("ResetBurn")
    snow.get("Config").set("Enabled",false)
    await create_timer(0.1).timeout
    for tree in samples.values():
        check(tree.get_node("Sprite").texture==tree.get("SelectedVariant").get("Texture"),"reset restores selected healthy variant")
        check(tree.get_node("HitResponse").get("HealthyLeaves"),"reset foliage")
    # A third species and asymmetric second artwork layout, created only as Resource data.
    var third=load("res://WORLD/Content/Vegetation/Trees/PinkBlossom/PinkBlossom.tres").duplicate()
    third.set("Id","tree.resource_only_test")
    third.set("HasFoliage",false)
    third.set("CanBurn",false)
    third.set("HitResponseEnabled",false)
    third.set("WindMaterial",null)
    var first=third.get("VisualVariants")[0]
    var variant=first.duplicate()
    variant.set("Id","asymmetric_variant")
    variant.set("SourceAnchor",Vector2(41,180))
    variant.set("SpriteScale",Vector2(0.7,0.8))
    variant.set("SpriteOffset",Vector2(3,-4))
    variant.set("Centered",true)
    variant.set("VisualMinimum",Vector2(-40,-170))
    variant.set("VisualMaximum",Vector2(100,40))
    variant.set("TrunkPolygon",PackedVector2Array([Vector2(-8,0),Vector2(-3,-16),Vector2(21,0)]))
    third.set("VisualVariants",third.get("VisualVariants").duplicate())
    third.get("VisualVariants").append(variant)
    for mirrored in [false,true]:
        var extra=load(TEMPLATE).instantiate()
        extra.call("Configure",third,1,mirrored)
        world.get_node("WorldActorsRoot/PreviewSceneryRoot").add_child(extra)
        check(extra.get("SelectedVariant")==variant,"second variant selected without scene/script")
        check(anchor(extra.get_node("Sprite"),variant.get("SourceAnchor")).distance_to(extra.global_position)<0.001,"asymmetric centered/offset mirrored source anchor")
        check(not extra.get_node("Burnable").call("TryIgnite",extra.global_position,"Disabled"),"optional fire disabled")
        check(not extra.get_node("HitResponse").is_in_group("world_tool_targets"),"optional hit disabled")
        check(not extra.get_node("HitResponse").get("HealthyLeaves"),"leafless species never sheds leaves")
        extra.get_node("Burnable").call("ResetBurn")
        check(not extra.get_node("HitResponse").get("HealthyLeaves"),"reset preserves leafless species")
        extra.queue_free()
    world.queue_free()
    await process_frame
    # Same world seed replays species, variants, position, scale, mirror and selected colliders.
    world=make_world()
    check(signature(trees_in(world))==initial,"deterministic world replay")
    world.queue_free()
    await process_frame
    # Seeded generation selects both data variants; no species PackedScene is present.
    var fixture_signature=[]
    for repetition in 2:
        world=make_world(third)
        var generated=trees_in(world)
        var choices={}
        for tree in generated:
            if tree.get_meta("world_content_id")=="tree.resource_only_test":
                choices[tree.get_meta("world_tree_variant")]=true
                check(anchor(tree.get_node("Sprite"),tree.get("SelectedVariant").get("SourceAnchor")).distance_to(tree.global_position)<0.001,"generated variant anchor")
        check(choices.size()==2,"both resource variants selected by seeded placement")
        if repetition==0:fixture_signature=signature(generated)
        else:check(signature(generated)==fixture_signature,"seeded variant choices replay")
        world.queue_free()
        await process_frame
    print("GENERIC TREE CHECKS: "+str(failures)+" failures")
    quit(1 if failures else 0)
