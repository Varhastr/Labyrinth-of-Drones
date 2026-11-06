# Loot, Inventory & Crafting – Implementation Notes

**Authoritative specification:** `SPEC_LootInventoryCrafting.md` (currently **v1.4**)

This README is supporting material only. In any conflict, the SPEC wins.

## Current Status

- Specification is under Quality Engineering review (v1.4).
- Scaffolding on this branch is **not** yet aligned with v1.4 rules.
- Do **not** begin feature implementation until the specification is approved.

## Key Rules (v1.4)

1. **True reservation / escrow with capacity accounting**  
   Placement uses `TryReserve` → `CommitReservation` / `RefundReservation`.  
   Reserved resources count toward the 999 total capacity. Pickups collected during a reservation are limited by remaining capacity after both spendable and reserved amounts. This guarantees exact refunds.

2. **Safe ghost creation**  
   Preview objects must be instantiated inactive (or under an inactive parent).  
   Component discovery must use `GetComponentsInChildren<T>(true)` so inactive children are included.  
   Gameplay scripts are disabled before any activation. Disabling components after a normal Instantiate is not sufficient.

3. **LootDropper idempotency**  
   `DropLoot()` must contain a per-instance one-time guard. Repeated calls on the same enemy produce drops only once.

4. **Legacy loot integration**  
   When an enabled `LootDropper` is present, the existing `LootPrefab` / `DropRate` path must be skipped for that enemy. Only a single call to `DropLoot()` is added to the death path.

## After Specification Approval

Once v1.4 (or a later approved version) is signed off:

1. Update the scaffolding code to match the SPEC exactly (especially Inventory reservation API + capacity rules, StructurePlacer ghost creation with inactive-child discovery, and LootDropper guard).
2. Create the required prefabs, LootTable, and four starter recipes.
3. Wire the single `DropLoot()` call + legacy-skip into the existing enemy death path.
4. Implement UI and run the full acceptance / edge-case test list in Section 8 of the SPEC.

## Folder Layout (unchanged)

```
Assets/ModAssets/Scripts/
  Inventory/
  Loot/
  Crafting/
  SPEC_LootInventoryCrafting.md   ← authoritative
  README_LootInventoryCrafting.md ← this file
```
