# Specification: Loot Drops, Inventory & Crafting System

**Document Status:** Authoritative Specification  
**Version:** 1.4  
**Project:** Labyrinth of Drones  
**Target Engine:** Unity (FPS Microgame base)  
**Audience:** Junior Engineering  
**Last Updated:** 2026-11-06  
**Supersedes:** Version 1.3

This document is the single source of truth for the Loot, Inventory, and Crafting systems. All implementation must conform to this specification. Scaffolding code and any README checklists are supporting materials only and do not override this document.

---

## 1. Purpose & Scope

### 1.1 Purpose
Introduce a resource economy that turns defeated drones into progression. Players collect two primary resources (Scrap and Circuitry), store them in an inventory, and spend them to craft weapons, placeable structures, and utility items.

This system is the foundation for later features (base building, survivor support, longer-term progression).

### 1.2 In Scope (v1)
- Resource drops from enemies on death via the new LootDropper system
- Simple inventory tracking two integer resources
- Crafting menu driven by ScriptableObject recipes
- Instantiation of weapon pickups
- Ghost-preview placement of structures with a true reservation model that guarantees exact refunds and respects total capacity
- Basic HUD display of current resources
- Safe transaction handling (validate before spend, rollback on failure)

### 1.3 Explicitly Out of Scope (v1)
- Item slots, equipment, or general-purpose item inventory
- Weight, stacking limits beyond the hard 999 cap, or inventory capacity
- Saving / loading of inventory state across sessions
- Multi-step crafting, crafting stations that must be powered, or research trees
- Complex building grid, rotation gizmos, or advanced validation (slope, overlapping volumes, NavMesh queries beyond basic raycast)
- Interaction between crafted objects and survivors
- Any modification or replacement of the existing enemy health / death system beyond a single method call and controlled suppression of the legacy loot path

---

## 2. Resources

| Resource    | Type     | Max Value | Source                          | Notes |
|-------------|----------|-----------|---------------------------------|-------|
| Scrap       | Integer  | 999       | Enemy death (common)            | Primary crafting material |
| Circuitry   | Integer  | 999       | Enemy death (less common)       | Higher-tier crafting material |
| Health Pack | Instant  | N/A       | Enemy death or world spawn      | Does **not** enter inventory. Instant heal on pickup. |

### 2.1 Health Pack Relationship to Legacy System
The existing microgame health pickup (used by the legacy `LootPrefab` / `DropRate` path) remains the Health Pack for v1.  
When the new LootDropper system is active on an enemy, the legacy loot path must be suppressed for that enemy so that Health Packs are only produced through the new LootTable if the designer explicitly includes them.  
This prevents duplicate health drops.

Resources never go below zero. Attempts to spend more than currently held must fail cleanly and leave inventory unchanged.

---

## 3. Inventory

### 3.1 Responsibilities
- Store current Scrap and Circuitry counts
- Provide atomic spend operations
- Support a true reservation (escrow) model for placement that respects total capacity
- Notify listeners when spendable values change
- Support a full reset

### 3.2 Interface (required)

```csharp
public static class Inventory
{
    public static int Scrap { get; }          // currently spendable
    public static int Circuitry { get; }      // currently spendable

    public static event System.Action OnInventoryChanged;

    public static void AddScrap(int amount);
    public static void AddCircuitry(int amount);
    public static bool TrySpend(int scrapCost, int circuitryCost);

    // Reservation API (required for placement)
    public static bool TryReserve(int scrapCost, int circuitryCost);
    public static void CommitReservation();
    public static void RefundReservation();

    public static void Reset();
}
```

### 3.3 Rules

- The hard cap of **999 applies to the combined total of spendable + reserved** resources for each type.
- Spendable values are what the player can currently use for new crafts or see on the HUD.
- `AddScrap` / `AddCircuitry`:
  - If `amount <= 0`, the call is a no-op (no event raised).
  - Available capacity = `999 - (spendable + reserved)`.
  - Only up to the remaining capacity is added. Excess is discarded (no error).
  - Raises `OnInventoryChanged` only when the spendable value actually changes.
- `TrySpend(scrapCost, circuitryCost)`:
  - If either cost is **negative**, return `false` immediately and leave inventory unchanged.
  - If either cost is zero, that resource is not required.
  - Zero-cost recipes (both costs == 0) are **allowed**.
  - If the player does not have enough spendable of either resource, return `false` and leave inventory unchanged.
  - On success, deduct both amounts from the spendable pools atomically and raise `OnInventoryChanged`.
- `OnInventoryChanged` must be raised after every successful mutation of spendable amounts.
- Inventory is static and lives for the duration of a play session / run.
- **Reset timing:** Call `Inventory.Reset()` on player death **and** when a new run / game session is started. Both events must clear spendable amounts and any active reservation.

### 3.4 Reservation / Escrow Model (Required)

**Capacity rule (critical):**  
Reserved resources **count toward the 999 total capacity**. While a reservation is active, pickups may only fill the remaining capacity after both spendable and reserved amounts are taken into account. This guarantees that a later refund can always restore the exact reserved quantity without exceeding the cap.

Example:
- Start: Scrap spendable = 990, reserved = 0.
- `TryReserve(10)` → spendable = 980, reserved = 10. Total = 990.
- Player collects Scrap. Only 9 more can be accepted (990 + 9 = 999). Further pickups are discarded.
- On cancel, `RefundReservation()` moves the 10 from reserved back to spendable → spendable = 989, reserved = 0. Exact restoration, still ≤ 999.

**API behaviour:**

- `TryReserve(scrap, circuitry)`:
  - Performs the same validation as `TrySpend` against **spendable** amounts.
  - On success, deducts the amounts from the spendable pools **and** records them in the internal reservation.
  - Only one reservation may exist at a time. If a reservation is already active, `TryReserve` must return `false` (preferred) or first refund the previous reservation—implementation must pick one policy and document it.
  - Raises `OnInventoryChanged` because spendable amounts have changed.
- While a reservation is active:
  - The reserved amounts are **not** available for new spends or new reservations.
  - HUD and affordability checks must display and use only the spendable (non-reserved) amounts.
  - `AddScrap` / `AddCircuitry` must calculate remaining capacity as `999 - (spendable + reserved)`.
- `CommitReservation()`:
  - Clears the reservation record. The resources stay spent (they are no longer reserved and are not returned). No change to spendable values occurs.
  - Must be safe to call only once per reservation.
- `RefundReservation()`:
  - Moves the exact reserved amounts back into the spendable pools.
  - Because capacity was reserved, this addition cannot exceed 999.
  - Clears the reservation record.
  - Raises `OnInventoryChanged`.
  - Must be safe to call only once per reservation.
- Confirm, cancel, and failure paths must settle a reservation **exactly once**. Double-commit or double-refund is forbidden.

---

## 4. Loot System

### 4.1 Loot Table
A ScriptableObject that defines a list of possible drops.

Each `LootEntry` contains:
- `pickupPrefab` – the prefab to instantiate (must have the correct pickup script)
- `chance` – probability in the closed range [0, 1]
- `minAmount` – inclusive minimum number of pickup instances to spawn on success
- `maxAmount` – inclusive maximum number of pickup instances to spawn on success

**Validation rules (must be enforced at edit time or runtime):**
- `chance` must be in [0, 1]
- `minAmount` and `maxAmount` must be ≥ 0
- `minAmount` ≤ `maxAmount`
- A reasonable upper spawn limit of 20 instances per entry is recommended to prevent performance spikes; exceeding this should log a warning.

Multiple entries may succeed on a single death. Each successful entry spawns its own set of instances independently.

### 4.2 Default v1 Loot Table Values
For the standard enemy LootTable used by HoverBots and Turrets:

| Entry       | Chance | Min | Max | Notes |
|-------------|--------|-----|-----|-------|
| Scrap       | 0.75   | 1   | 3   | Most common drop |
| Circuitry   | 0.35   | 1   | 1   | Less common |
| Health Pack | 0.15   | 1   | 1   | Optional; only if designer wants it via the new system |

These values are the required defaults for v1. Designers may create additional tables for special enemies later.

### 4.3 Drop Behaviour
When an enemy dies:
1. The `LootDropper` component on that enemy evaluates its assigned LootTable **at most once**.
2. For every entry whose random roll succeeds (`Random.value <= chance`), a number of instances is chosen uniformly from `[minAmount, maxAmount]` (inclusive).
3. Each instance is spawned near the enemy’s position (slight upward offset) and given a small upward + random horizontal impulse.

### 4.4 Idempotency Guard (Required)
`LootDropper` must contain a per-instance one-time guard (e.g. a private `bool hasDropped` flag set to true on the first successful call to `DropLoot()`).  
Subsequent calls to `DropLoot()` on the same instance must be ignored.  
This guarantees that duplicate death callbacks cannot produce duplicate drops.  
The guard lives on the component instance; it must not require changes to the broader enemy death system.

### 4.5 Integration with Existing Enemy Death System (Critical)

The existing `EnemyController.OnDie()` path already contains a legacy loot routine driven by `LootPrefab` and `DropRate`. The HoverBot prefab currently has a health pickup assigned at 100% drop rate.

**Required integration rules:**

- Add a single call to `GetComponent<LootDropper>()?.DropLoot();` inside the existing death path.
- When a `LootDropper` component is present and enabled on the enemy, the legacy `LootPrefab` / `DropRate` logic **must be skipped** for that enemy. This prevents duplicate drops.
- If no `LootDropper` is present, the legacy behaviour continues unchanged (backward compatible).
- Do **not** rewrite, replace, or significantly restructure the existing death / health system. Only the single method call and a conditional skip of the legacy drop are permitted.

### 4.6 Pickup Behaviour
- Pickups use a trigger collider.
- On contact with the player they call the appropriate `Inventory.Add…` method and destroy themselves.
- Pickups auto-destroy after a configurable lifetime (default 60 seconds) if not collected.
- Collecting a pickup when total capacity (spendable + reserved) is already at 999 simply leaves the value unchanged (no error, no overflow).

---

## 5. Crafting System

### 5.1 Recipe Definition
Recipes are ScriptableObjects. Required fields:

| Field            | Type              | Purpose |
|------------------|-------------------|---------|
| id               | string            | Unique stable key (see 5.1.1) |
| displayName      | string            | Player-facing name |
| description      | string            | Player-facing text |
| icon             | Sprite            | UI icon |
| scrapCost        | int               | Cost in Scrap (≥ 0) |
| circuitryCost    | int               | Cost in Circuitry (≥ 0) |
| resultType       | CraftResultType   | Determines post-craft behaviour |
| resultPrefab     | GameObject        | Prefab to spawn or place (required for WeaponPickup and Structure/Utility) |

`CraftResultType` values:
- `WeaponPickup` – spawn the prefab as a world pickup near the player
- `Structure` / `Utility` – enter placement mode
- `Consumable` – apply a concrete v1 effect (see 5.4)

#### 5.1.1 Recipe ID Rules
- `id` must be non-empty and unique among all recipes registered with the CraftingManager.
- On registration, if a duplicate `id` is detected, the new recipe is rejected and a warning is logged. The existing recipe is kept.

### 5.2 Recipe Registration
CraftingManager holds an explicit list of `CraftingRecipe` references assigned in the Inspector (or loaded at startup).  
There is no automatic discovery via Resources or Addressables in v1. All recipes used in the game must be present in this list.

### 5.3 Starter Recipes (required for v1)

| id                | Display Name      | Scrap | Circuitry | Result Type   | Notes |
|-------------------|-------------------|-------|-----------|---------------|-------|
| scrap_barrier     | Scrap Barrier     | 8     | 0         | Structure     | Simple waist-high cover. Prefab required. |
| makeshift_smg     | Makeshift SMG     | 15    | 4         | WeaponPickup  | Spawns an existing or new weapon pickup prefab. |
| auto_turret_mk1   | Auto-Turret Mk1   | 25    | 12        | Structure     | Placeable defensive turret (may reuse existing turret prefab as placeholder). |
| field_medkit      | Field Medkit      | 5     | 2         | Consumable    | Instantly restores a fixed amount of player health (see 5.4). |

These four recipes must exist as ScriptableObject assets and be registered with the CraftingManager for the acceptance tests to pass.

### 5.4 Consumable Behaviour (v1)
For `CraftResultType.Consumable` the only supported effect in v1 is an instant heal.  
When a consumable recipe is successfully crafted, restore a fixed amount of health to the player (recommended default: 25–40 points, configurable on the recipe or a constant).  
If the player health component cannot be found, the craft must fail and resources must not be spent (or must be rolled back).

### 5.5 Crafting Transaction Rules (Critical)

A craft is considered successful only when the requested output has been created **or** placement mode has successfully begun.

**Required sequence:**

1. Validate all dependencies **before** any resources are spent or reserved:
   - Recipe is non-null and registered.
   - Costs are ≥ 0.
   - For `WeaponPickup` and `Structure`/`Utility`: `resultPrefab` is non-null.
   - For `Structure`/`Utility`: a valid `StructurePlacer` reference exists and is ready.
   - For `Consumable`: player health component can be located.
   - Player reference (if needed for spawning) can be located.
2. If any validation fails → abort, log a warning, leave inventory unchanged, return failure.
3. For `WeaponPickup` and `Consumable`: call `Inventory.TrySpend`.  
   For `Structure`/`Utility`: call `Inventory.TryReserve`.
   If the call returns false → abort, return failure.
4. Attempt to create the result or begin placement.
5. If step 4 fails for any reason → **immediately roll back**:
   - For spend-based crafts: refund via `AddScrap` / `AddCircuitry`.
   - For reserved crafts: call `RefundReservation()`.
   Return failure.
6. Only after step 4 succeeds is the craft considered complete.  
   For structures, the reservation remains active until the player confirms placement (`CommitReservation`) or cancels (`RefundReservation`).

### 5.6 Structure Placement

#### 5.6.1 Safe Ghost Creation Sequence (Required)
Ordinary `Instantiate()` followed by disabling components is **not** safe: `Awake` and `OnEnable` can already have run.

**Required sequence:**

1. Instantiate the preview prefab in an **inactive** state (or under an inactive parent) so that `Awake`/`OnEnable` do not run yet.
2. Discover **all** colliders, rigidbodies, and gameplay `MonoBehaviour`s across the full hierarchy, including inactive children, using `GetComponentsInChildren<T>(true)` (the `true` argument is mandatory).
3. Disable all discovered colliders, set all discovered Rigidbodies to kinematic, and disable every discovered gameplay `MonoBehaviour` that could execute game logic.
4. Preserve rendering components (MeshRenderer, SkinnedMeshRenderer, etc.) so the ghost remains visible.
5. Only after the above safeguards are in place may the ghost be activated (if activation is required for rendering).
6. The ghost must remain non-interactive for the entire preview lifetime.

Disabling colliders and making rigidbodies kinematic alone is insufficient. Component discovery must include inactive descendants.

#### 5.6.2 Placement Flow
- Ghost follows a raycast from the main camera against a configurable LayerMask, clamped to a maximum distance (default 8 m).
- Recommended LayerMask excludes the player, enemies, and triggers; it should primarily hit world geometry / ground.
- Left mouse button places the real prefab at the hit point (if valid), calls `CommitReservation()`, and exits placement mode.
- Right mouse button or Escape cancels placement, calls `RefundReservation()`, and exits placement mode.
- Only one placement reservation may be active at a time.

---

## 6. User Interface Requirements

### 6.1 Inventory HUD
- Always visible during gameplay (or easily toggled).
- Displays current **spendable** Scrap and Circuitry counts (i.e. values after any active reservation has been deducted).
- Updates immediately when `OnInventoryChanged` fires.

### 6.2 Crafting Menu
- Opened / closed by a single configurable key (default recommendation: C).
- Lists every recipe registered with the CraftingManager.
- Each entry shows name, description, costs, and icon.
- Craft button is disabled when the player cannot afford the recipe or when a reservation is already active.
- Menu should close (or become non-interactive) when placement mode begins.

UI technology may be Unity UI or TextMeshPro. Visual polish is secondary to functional correctness in v1.

---

## 7. Data & Folder Conventions

All new code lives under:

```
Assets/ModAssets/Scripts/
  Inventory/
  Loot/
  Crafting/
```

All new C# types use the namespace `LabyrinthOfDrones`.

ScriptableObject assets (LootTables, Recipes) should be placed under:
```
Assets/ModAssets/ScriptableObjects/
```

Pickup and crafted result prefabs should be placed under:
```
Assets/ModAssets/Prefabs/Loot/
Assets/ModAssets/Prefabs/Crafted/
```

---

## 8. Acceptance Criteria & Required Test Cases

The system is considered complete for v1 when all of the following pass.

### 8.1 Functional Acceptance
1. Killing an enemy that has a correctly configured LootDropper produces the expected pickups with impulse and does **not** also produce the legacy LootPrefab drop.
2. Walking over Scrap and Circuitry pickups increases the corresponding spendable counters and the HUD updates. Collecting when total capacity (spendable + reserved) is already at 999 leaves the value unchanged.
3. The crafting menu opens/closes with the configured key and lists all registered recipes.
4. Recipes the player cannot afford have a disabled craft button.
5. Crafting a WeaponPickup recipe spawns the prefab near the player and deducts the correct resources.
6. Crafting a Structure recipe enters placement mode, shows a non-interactive ghost created via the safe inactive sequence (including inactive children), and allows placement with left click.
7. Cancelling placement restores the exact reserved resources via `RefundReservation`.
8. Inventory spendable values never go negative or exceed 999 under any tested sequence. Total (spendable + reserved) never exceeds 999.
9. Only a single call to `DropLoot()` plus a conditional skip of the legacy drop has been added to the death path.

### 8.2 Required Edge-Case / Safety Tests
These must be explicitly verified:

- Negative spend / reserve costs are rejected; inventory is unchanged.
- Zero-cost recipes succeed (if all other validation passes).
- Negative or zero addition amounts are ignored.
- Inventory is correctly clamped at both 0 and 999; reserved amounts count toward the cap.
- During an active reservation, pickups cannot push total (spendable + reserved) above 999.
- Duplicate calls to `DropLoot()` on the same enemy instance produce drops only once (idempotency guard).
- Missing player reference, missing resultPrefab, or missing StructurePlacer causes the craft to fail with **no** resource loss.
- Placement cancellation restores the exact reserved cost even if the player collects additional resources while placing.
- Beginning a second placement while one is active either refunds the first or rejects the second; resources are never double-spent or double-refunded.
- Invalid loot-table ranges (min > max, negative amounts, chance outside 0–1) are rejected or safely clamped with a warning.
- Ghost previews never execute gameplay behaviour (`Awake`/`OnEnable` of gameplay scripts do not run, including on inactive children; no shooting, no triggers, no damage).
- Component discovery on the ghost uses `GetComponentsInChildren<T>(true)`.
- Recipe duplicate IDs are rejected on registration.
- `CommitReservation` and `RefundReservation` each settle a reservation exactly once.

---

## 9. Future Extension Points (Informative Only)

The following are deliberately left open for later versions and must not be hard-coded against in v1:

- Per-enemy or per-wave loot table overrides
- Additional resource types
- Persistent inventory across runs
- Crafting stations that must be built and powered
- Survivor AI interacting with placed structures
- Advanced placement validation and building grids

---

## 10. Document Control

This specification (v1.4) supersedes version 1.3 and all earlier informal descriptions.  
Changes to behaviour require an update to this document and a new version number.  
Implementation may begin only after this version has been reviewed and approved.
