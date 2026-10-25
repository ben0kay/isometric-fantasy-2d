# Wooden Log

`WoodenLog.tres` uses shared ItemDefinition with ID `wooden_log`, MaxStack 40 and an illustrative unit weight of 1 kg. These values are authorable balancing data, not finalized carry restrictions. It has no equipment behavior.

`WoodenLog.svg` is simple temporary transparent inventory artwork; replace its Icon reference with a final PNG when available. No unique item script or scene is needed. The item is registered in DefaultItemCatalog; InventoryPractice explicitly grants 12 logs and offers a factory button to add five. HarvestingPractice now demonstrates four axe chops yielding 3–5 log units through shared reward tables and pickups. Normal World/campaign trees still do not yield logs until H4 persistence.
