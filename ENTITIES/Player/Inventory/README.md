# Player / Inventory

Future player inventory orchestration: carried items, hotbar, selection, and player-specific access to containers.

Item definitions and reusable inventory/container mechanics should live in their appropriate shared owning systems, not be copied per actor.

**Status:** Player-specific orchestration remains planned. Reusable item storage/hotbar models now live in `SYSTEMS/Inventory`; `TESTS/Inventory/InventoryPractice` supplies a disposable adapter. Complete persistent player ownership/save integration before creating a production adapter here. Do not duplicate storage rules inside Player or UI.
