# Shared enemies

`Enemy.tscn` + `EnemyActor.cs` implement common navigation, axe/tool hit reception, health, damage flash and death fade. `EnemyDefinition.cs` owns data-driven visual, collision, hit radius and baseline pursuit parameters. Author species/variants as Resources and artwork, not individual scenes. This first shared actor implements ground pursuit; later flying, ranged or unusual locomotion should be pluggable behaviour modules rather than an enormous if/switch or duplicated enemy scene.
