# Shared item definitions and catalog

`ItemDefinition` holds stable ID, display name, description, icon, unit weight, stack limit and optional existing equipment profile. Instance quantities/stack keys live in shared runtime storage. New materials require Resources/artwork, not new scripts or scenes. Icons may differ from held/world artwork.

`ItemCatalog` validates explicit Resources and builds one cached ordinal-ID lookup. Duplicate IDs, missing definitions, invalid mass/stack limits and incomplete equipment references fail registration. Initialization is atomic; invalid catalogs expose no partial lookup. It never scans folders during gameplay or mutates definitions to generate fallback icons. Missing icons stay blank with descriptive tooltips. Storage accepts only the canonical registered Resource, preventing duplicate IDs from spoofing different properties.

## Add a material

1. Put its `.tres` and artwork in the appropriate named content folder, e.g. `ITEMS/Resources/Wood/WoodenLog/`.
2. Use ItemDefinition, a unique stable ID and explicit display/icon/weight/stack values. Keep Equipment empty for ordinary materials.
3. Add the Resource to `ITEMS/DefaultItemCatalog.tres` Items. Creating a file alone does not register it; registering it alone does not spawn/grant it.
4. Run `TESTS/Inventory/ValidateItemInventory.tscn` and use a disposable fixture when tuning.

Never rename IDs just because paths/display labels move. Future saves must resolve canonical definitions by ID and include an explicit migration/content-version policy. This pass adds no general inventory persistence.

## Future Avalonia authoring

The `.tres` definitions/catalog remain authoritative. An external editor should author these shared fields/artwork references, update explicit registration, validate with Godot and preserve unrelated fields/IDs. No second editor-only item database, per-item C# generation, runtime discovery, external loader or content-pack support is implemented yet. Start with a flat catalog; split into category catalogs only when actual content warrants it.
