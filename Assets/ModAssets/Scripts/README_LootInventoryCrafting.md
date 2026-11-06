# Loot, Inventory & Crafting – Implementation Notes

**Authoritative specification:** `SPEC_LootInventoryCrafting.md` (currently **v1.3**)

This README is supporting material only. In any conflict, the SPEC wins.

## Current Status

- Specification is under Quality Engineering review (v1.3).
- Scaffolding on this branch is **not** yet aligned with v1.3 rules.
- Do **not** begin feature implementation until the specification is approved.

## Key Rules Introduced / Tightened in v1.3

1. **True reservation / escrow model**  
   Placement uses `TryReserve` → `CommitReservation` / `RefundReservation`.  
   Simple “spend then Add back” is forbidden because it can lose resources at the 999 cap.

2. **Safe ghost creation**  
   Preview objects must be instantiated inactive (or under an inactive parent) so that `Awake`/`OnEnable` of gameplay scripts never run. Disabling components after a normal Instantiate is not sufficient.

3. **LootDropper idempotency**  
   `DropLoot()` must contain a per-instance one-time guard. Repeated calls on the same enemy produce drops only once.

4. **Legacy loot integration**  
   When an enabled `LootDropper` is present, the existing `LootPrefab` / `DropRate` path must be skipped for that enemy. Only a single call to `DropLoot()` is added to the death path.

## After Specification Approval

Once v1.3 (or a later approved version) is signed off:

1. Update the scaffolding code to match the SPEC exactly (especially Inventory reservation API, StructurePlacer ghost creation, and LootDropper guard).
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
