# Reversible fixed-seed production comparison; no save files or project settings are modified.
extends SceneTree
func _initialize(): call_deferred("run")
func run():
    var fingerprints = []
    for enabled in [false, true]:
        var before = Time.get_ticks_msec()
        var world = load("res://SCENES/World/World.tscn").instantiate()
        world.Config = world.Config.duplicate()
        world.Config.Streaming = null # Explicit compatibility-preview baseline.
        world.Config.RandomSeedOnLoad = false
        world.Config.FixedSeed = 213876
        if not enabled: world.Config.Generation = null
        root.add_child(world)
        var startup = Time.get_ticks_msec() - before
        var signature = []
        for actor in world.get_node("Actors").get_children():
            if actor.has_meta("world_content_id"): signature.append([actor.get_meta("world_content_id"),actor.position,actor.scale])
        fingerprints.append(hash(signature))
        var frame_total = 0.0
        var peak_nodes = 0.0
        var peak_memory = 0.0
        for i in range(30):
            await process_frame
            frame_total += Performance.get_monitor(Performance.TIME_PROCESS)
            peak_nodes = max(peak_nodes, Performance.get_monitor(Performance.OBJECT_NODE_COUNT))
            peak_memory = max(peak_memory, Performance.get_monitor(Performance.MEMORY_STATIC))
        print("P0 MEASURE macro=",enabled," startup_ms=",startup," scenery=",signature.size()," nodes=",world.get_child_count(true)," tree_nodes=",peak_nodes," sampled_peak_static_memory=",peak_memory," avg_process_ms=",frame_total/30*1000," fingerprint=",fingerprints[-1])
        world.queue_free()
        await process_frame
        await process_frame
    assert(fingerprints[0] == fingerprints[1], "Production placement changed")
    print("P0 PREVIEW BASELINE PASS")
    quit()
