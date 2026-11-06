# Loot, Inventory & Crafting System – Scaffolding (v1)

This folder contains the complete scaffolding for the core resource loop of Labyrinth of Drones.

**Audience:** Junior engineering  
**Status:** Ready for implementation & testing

## What is included

- `Inventory/` – Static inventory for Scrap + Circuitry + simple HUD
- `Loot/` – LootTable, LootDropper, ScrapPickup, CircuitryPickup
- `Crafting/` – CraftingRecipe (ScriptableObject), CraftingManager, StructurePlacer, CraftingUI

## Integration Checklist (do in this order)

1. Create the missing folders if Unity did not auto-generate them:
   - `Assets/ModAssets/Prefabs/Loot/`
   - `Assets/ModAssets/Prefabs/Crafted/`
   - `Assets/ModAssets/ScriptableObjects/Recipes/`

2. Create two simple pickup prefabs:
   - ScrapPickup.prefab (Collider = Trigger, Rigidbody, ScrapPickup script)
   - CircuitryPickup.prefab (same, but CircuitryPickup script)

3. Create a LootTable ScriptableObject and assign the two pickup prefabs with the default chances from the spec (Scrap ~0.75, Circuitry ~0.35).

4. Add the `LootDropper` component to every enemy prefab (HoverBot, Turret, etc.) and assign the LootTable.

5. **Critical:** Wire `DropLoot()` into the existing enemy death logic.  
   Look for the death / OnDie handling inside `EnemyController` (or equivalent) and add:
   ```csharp
   GetComponent<LootDropper>()?.DropLoot();
   ```
   Do **not** create a new death system.

6. Create a GameObject in the scene with:
   - CraftingManager
   - StructurePlacer
   Assign any starter recipes to the CraftingManager list.

7. Build a minimal UI:
   - InventoryUI on the HUD (two text fields for Scrap / Circuitry)
   - CraftingUI + a simple recipe entry prefab that uses `RecipeEntryUI`

8. Create at least the four starter recipes listed in the original spec (Scrap Barrier, Makeshift SMG, Auto-Turret Mk1, Field Medkit).

9. Test the full loop:
   - Kill an enemy → pickups appear and fly outward
   - Walk over them → resource counters increase
   - Press C (or configured key) → crafting menu opens
   - Craft a structure → enter placement mode
   - Left-click to place, Right-click / Escape to cancel (resources refunded)

## Non-Goals for v1
- No inventory slots / item list beyond the two resource counters
- No save/load yet
- No complex building grid or rotation gizmo
- No power requirements or crafting stations that must be built first
- Do not rewrite the existing enemy health or death system

## Next Steps after this works
- More interesting recipes
- Better placement validation (slope, overlap, NavMesh)
- Visual feedback (floating +X text, sounds, particles)
- Survivor recruitment + base building (future systems)

Once the full loop is working and tested, open a PR or notify the team so the feature can be reviewed and merged.
