# Tree hit feedback test

`TreeHitFeedback` is a child of the oak tree scene. Future tree scenes can opt in with the same helper, a Sprite child, a TrunkFootprint collision polygon and a per-species config. It implements the weapon hit contract without knowing about axe input or attack animation.

`TreeHitMasterConfig` / `DefaultTreeHitConfig.tres` own wobble duration/angle, leaf count and leaf colour. Set LeavesPerHit to zero for a leafless receiver. Each successful hit briefly rotates the artwork around its base (2.8° maximum setting, decaying over 0.65 seconds), leaving collision/root position unchanged. Existing GPU wind, snow materials and projected shadow handling remain intact.

A lazily allocated 24-slot leaf pool per struck tree gives a local canopy burst; repeated hits reuse/overwrite slots. Leaves integrate current shared Weather wind and expire after a few seconds. Ambient Atmosphere leaves are independent; disabling ambient leaves does not suppress hit feedback. Leaf origins are sampled from the sprite's upper rectangle, rather than a precise foliage mask; future species should tune colour/art or add a mask if needed. There is no permanent foliage loss, health or loot.

Idle receivers have processing disabled. Only struck trees update while wobbling/leaves remain; the original transform is restored on completion/removal. Projected shadows read that sprite transform as usual. For extraction, move the hit contract/query to shared interaction/combat and this receiver/config to tree presentation; keep ambient weather leaves separate.
