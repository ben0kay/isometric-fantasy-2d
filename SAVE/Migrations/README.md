# Save migrations — compatibility gate implemented, conversions deferred

`SaveCompatibility` accepts durable schema 1, blueprint schema 4 and the current macro/feature/local generator versions. Earlier blueprints were session contracts, not shipped durable campaigns. Unknown versions fail explicitly and preserve source files. No migration that rerolls saved geography is permitted. A deliberate converter and content-upgrade policy must be authored/tested when a supported previous durable format actually needs conversion.

Current text-resource fingerprint changes also require an explicit migration; seed-policy changes alone do not. P4 compilation/runtime validation remains deferred to the local Copilot handoff.
