# Disposable fixed-seed F6 crowd comparison; in-memory overrides only, no campaign access.
extends SceneTree
func _initialize(): call_deferred("run")
func run():
    var movement=load("res://ENTITIES/Shared/Movement/DefaultMovementConfig.tres")
    movement.set("Enabled",not OS.get_cmdline_user_args().has("--separation-off"))
    print("SEPARATION enabled=",movement.get("Enabled"))
    var world=load("res://SCENES/World/World.tscn").instantiate()
    world.set("Config",world.get("Config").duplicate())
    world.get("Config").set("RandomSeedOnLoad",false)
    world.get("Config").set("FixedSeed",213876)
    var player=world.get_node("WorldActorsRoot/PersistentEntitiesRoot/Player")
    var config=player.get("Config").duplicate()
    config.set("MaxHealth",10000)
    config.set("ReactionImmune",true)
    player.set("Config",config)
    root.add_child(world)
    var actors=[]
    for child in world.get("TestActorsRoot").get_children():
        if child is CharacterBody2D: actors.append(child)
    print("ES1 actors=",actors.size()," seed=",world.get("Config").get("FixedSeed"))
    var physics_ms=[]
    var min_gap=INF
    var crowded=0
    var pair_samples=0
    var previous=[]
    var queued=0
    var far_samples=0
    for a in actors: previous.append(a.global_position)
    for frame in range(600):
        await physics_frame
        physics_ms.append(Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS)*1000.0)
        for i in range(actors.size()):
            var a=actors[i]
            for j in range(i+1,actors.size()):
                var gap=a.global_position.distance_to(actors[j].global_position)
                min_gap=min(min_gap,gap)
                if frame>=300:
                    pair_samples+=1
                    if gap<24: crowded+=1
            if frame>=300 and a.global_position.distance_to(player.global_position)>60:
                far_samples+=1
                if a.global_position.distance_to(previous[i])<0.25: queued+=1
            previous[i]=a.global_position
        if frame in [119,299,599]:
            var distances=[]
            for a in actors: distances.append(snapped(a.global_position.distance_to(player.global_position),0.1))
            print("ES1 t=",(frame+1)/60.0," distances=",distances)
    physics_ms.sort()
    var sum=0.0
    for ms in physics_ms: sum+=ms
    print("ES1 WORLD min_pair_px=",min_gap," close_pair_samples=",crowded,"/",pair_samples," stalled_far_samples=",queued,"/",far_samples," physics_mean_ms=",sum/physics_ms.size()," physics_p95_ms=",physics_ms[int(physics_ms.size()*0.95)]," physics_max_ms=",physics_ms[-1])
    quit()
