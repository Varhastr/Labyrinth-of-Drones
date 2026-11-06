# Loot, Inventory & Crafting – Implementation Notes

**Authoritative specification:** `SPEC_LootInventoryCrafting.md` (currently **v1.6**)

This README is supporting material only. In any conflict, the SPEC wins.

## Current Status

- Specification is under Quality Engineering review (v1.6).
- Scaffolding on this branch is **not** yet aligned with v1.6 rules.
- Do **not** begin feature implementation until the specification is approved.

## Key Rules (v1.6)

1. **True reservation / escrow with capacity accounting**  
   Placement uses `TryReserve` → `CommitReservation` / `RefundReservation`.  
   Reserved resources count toward the 999 total capacity. Pickups collected during a reservation are limited by remaining capacity after both spendable and reserved amounts. This guarantees exact refunds.  
   Corrected §3.4 example: starting from 990 Scrap, reserving 10, then collecting 9 (spendable 989, reserved 10, total 999), a refund restores spendable to **999** (reserved 0, total 999).  
   Reservation activity is tracked by an explicit active flag, never inferred from reserved amounts — a zero-cost reservation is a real active reservation.

2. **Overlapping reservations are rejected**  
   If a reservation is already active, `TryReserve` must return `false` without changing inventory or the existing reservation. The current reservation remains intact until it is explicitly committed or refunded. This is a fixed specification policy, not an implementation choice.

3. **Read-only reservation state**  
   `Inventory.IsReservationActive` exposes whether a reservation is active. It is `true` after a successful `TryReserve` (including zero-cost) and `false` after commit, refund, or reset. CraftingManager/UI use it to disable crafting while placement owns the reservation. Commit and refund raise `OnInventoryChanged` when they settle an active reservation; calling either with no active reservation is a safe no-op.

4. **Safe ghost preview is visual-only**  
   The gameplay prefab is never instantiated during preview — disabling components after instantiation cannot guarantee `Awake` / `OnEnable` never run, so the old inactive-instantiation approach is explicitly forbidden.  
   The preview is a dedicated preview prefab (renderers/transforms only) or a runtime-built visual clone with no gameplay scripts, colliders, or rigidbodies. The real prefab is instantiated only on placement confirm.

5. **Loot probability boundaries are exact**  
   Chance exactly 0 never drops; chance exactly 1 always drops; otherwise the roll succeeds iff `Random.value < chance` (a `<=` comparison would let a 0% chance succeed, since `Random.value` can return exactly 0.0).

6. **Exact spend rollback**  
   A failed spend-based craft restores the exact spent amounts via the new synchronous `Inventory.RefundSpend` API — never through `AddScrap` / `AddCircuitry`, whose 999 clamp could truncate the refund. The rollback happens in the same call stack as the spend, before `TryCraft` returns.

7. **Reservation settlement on reset and abnormal exit**  
   Every reservation settles exactly once and never strands: confirm → commit; any other exit (cancel, failure, placer disabled/destroyed) → refund through a single guarded settle path. Player death / new-run `Reset()` settles the reservation as part of clearing state.

8. **LootDropper idempotency**  
   `DropLoot()` must contain a per-instance one-time guard. Repeated calls on the same enemy produce drops only once.

9. **Legacy loot integration**  
   When an enabled `LootDropper` is present, the existing `LootPrefab` / `DropRate` path must be skipped for that enemy. Only a single call to `DropLoot()` is added to the death path.

## After Specification Approval

Once v1.6 (or a later approved version) is signed off:

1. Update the scaffolding code to match the SPEC exactly (especially Inventory reservation API + capacity rules + `IsReservationActive` / `RefundSpend`, StructurePlacer visual-only ghost preview, and LootDropper guard).
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
