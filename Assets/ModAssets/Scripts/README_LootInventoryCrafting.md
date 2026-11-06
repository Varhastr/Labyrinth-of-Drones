# Loot, Inventory & Crafting – Implementation Notes

**Authoritative specification:** `SPEC_LootInventoryCrafting.md` (currently **v1.5**)

This README is supporting material only. In any conflict, the SPEC wins.

## Current Status

- Specification is under Quality Engineering review (v1.5).
- Scaffolding on this branch is **not** yet aligned with v1.5 rules.
- Do **not** begin feature implementation until the specification is approved.

## Key Rules (v1.5)

1. **True reservation / escrow with capacity accounting**  
   Placement uses `TryReserve` → `CommitReservation` / `RefundReservation`.  
   Reserved resources count toward the 999 total capacity. Pickups collected during a reservation are limited by remaining capacity after both spendable and reserved amounts. This guarantees exact refunds.  
   Corrected §3.4 example: starting from 990 Scrap, reserving 10, then collecting 9 (spendable 989, reserved 10, total 999), a refund restores spendable to **999** (reserved 0, total 999).

2. **Overlapping reservations are rejected**  
   If a reservation is already active, `TryReserve` must return `false` without changing inventory or the existing reservation. The current reservation remains intact until it is explicitly committed or refunded. This is a fixed specification policy, not an implementation choice.

3. **Safe ghost creation**  
   Preview objects must be instantiated inactive (or under an inactive parent).  
   Component discovery must use `GetComponentsInChildren<T>(true)` so inactive children are included.  
   Gameplay scripts are disabled before any activation. Disabling components after a normal Instantiate is not sufficient.

4. **LootDropper idempotency**  
   `DropLoot()` must contain a per-instance one-time guard. Repeated calls on the same enemy produce drops only once.

5. **Legacy loot integration**  
   When an enabled `LootDropper` is present, the existing `LootPrefab` / `DropRate` path must be skipped for that enemy. Only a single call to `DropLoot()` is added to the death path.

## After Specification Approval

Once v1.5 (or a later approved version) is signed off:

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
