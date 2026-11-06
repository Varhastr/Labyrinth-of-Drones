# Specification: Loot Drops, Inventory & Crafting System

**Document Status:** Authoritative Specification  
**Version:** 1.2  
**Project:** Labyrinth of Drones  
**Target Engine:** Unity (FPS Microgame base)  
**Audience:** Junior Engineering  
**Last Updated:** 2026-11-06  
**Supersedes:** Version 1.1

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
- Ghost-preview placement of structures with guaranteed refund on cancel
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
- Notify listeners when values change
- Support a full reset

### 3.2 Interface (required)

```csharp
public static class Inventory
{
    public static int Scrap { get; }
    public static int Circuitry { get; }

    public static event System.Action OnInventoryChanged;

    public static void AddScrap(int amount);
    public static void AddCircuitry(int amount);
    public static bool TrySpend(int scrapCost, int circuitryCost);
    public static void Reset();
}
```

### 3.3 Rules

- All mutations must keep values in the closed range **[0, 999]**.
- `AddScrap` / `AddCircuitry`:
  - If `amount <= 0`, the call is a no-op (no event raised).
  - Otherwise add the amount and clamp to 999.
- `TrySpend(scrapCost, circuitryCost)`:
  - If either cost is **negative**, return `false` immediately and leave inventory unchanged.
  - If either cost is zero, that resource is not required.
  - Zero-cost recipes (both costs == 0) are **allowed**.
  - If the player does not have enough of either resource, return `false` and leave inventory unchanged.
  - On success, deduct both amounts atomically and raise `OnInventoryChanged`.
- `OnInventoryChanged` must be raised after every successful mutation.
- Inventory is static and lives for the duration of a play session / run.
- **Reset timing:** Call `Inventory.Reset()` on player death **and** when a new run / game session is started. Both events must clear the inventory.

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
1. The `LootDropper` component on that enemy evaluates its assigned LootTable.
2. For every entry whose random roll succeeds (`Random.value <= chance`), a number of instances is chosen uniformly from `[minAmount, maxAmount]` (inclusive).
3. Each instance is spawned near the enemy’s position (slight upward offset) and given a small upward + random horizontal impulse.

### 4.4 Integration with Existing Enemy Death System (Critical)

The existing `EnemyController.OnDie()` path already contains a legacy loot routine driven by `LootPrefab` and `DropRate`. The HoverBot prefab currently has a health pickup assigned at 100% drop rate.

**Required integration rules:**

- Add a single call to `GetComponent<LootDropper>()?.DropLoot();` inside the existing death path.
- When a `LootDropper` component is present and enabled on the enemy, the legacy `LootPrefab` / `DropRate` logic **must be skipped** for that enemy. This prevents duplicate drops.
- If no `LootDropper` is present, the legacy behaviour continues unchanged (backward compatible).
- Do **not** rewrite, replace, or significantly restructure the existing death / health system. Only the single method call and a conditional skip of the legacy drop are permitted.

### 4.5 Pickup Behaviour
- Pickups use a trigger collider.
- On contact with the player they call the appropriate `Inventory.Add…` method and destroy themselves.
- Pickups auto-destroy after a configurable lifetime (default 60 seconds) if not collected.
- Collecting a pickup when the corresponding resource is already at 999 simply leaves the value at 999 (no error, no overflow).

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

1. Validate all dependencies **before** any resources are spent:
   - Recipe is non-null and registered.
   - Costs are ≥ 0.
   - For `WeaponPickup` and `Structure`/`Utility`: `resultPrefab` is non-null.
   - For `Structure`/`Utility`: a valid `StructurePlacer` reference exists and is ready.
   - For `Consumable`: player health component can be located.
   - Player reference (if needed for spawning) can be located.
2. If any validation fails → abort, log a warning, leave inventory unchanged, return failure.
3. Call `Inventory.TrySpend`. If it returns false → abort, return failure.
4. Attempt to create the result or begin placement.
5. If step 4 fails for any reason → **immediately roll back** the exact resources that were spent and return failure.
6. Only after step 4 succeeds is the craft considered complete.

### 5.6 Structure Placement & Refund Guarantee

#### 5.6.1 Placement Flow
- Entering placement mode creates a ghost instance of the result prefab.
- The ghost must have all colliders disabled and all Rigidbodies set to kinematic.
- **All MonoBehaviour scripts on the ghost and its children that could execute gameplay logic must be disabled** (or the ghost must be created in a way that prevents them from running). This prevents turrets from shooting, triggers from firing, etc., while the object is only a preview.
- Ghost follows a raycast from the main camera against a configurable LayerMask, clamped to a maximum distance (default 8 m).
- Recommended LayerMask excludes the player, enemies, and triggers; it should primarily hit world geometry / ground.
- Left mouse button places the real prefab at the hit point (if valid) and exits placement mode.
- Right mouse button or Escape cancels placement.

#### 5.6.2 Reservation / Escrow Model (Required)
To guarantee that cancellation always restores the exact cost even if the inventory reaches 999 while the player is placing:

- When placement mode begins successfully, the spent resources are considered **reserved**.
- The reservation records the exact scrap and circuitry amounts that were deducted.
- On successful placement the reservation is simply cleared (resources stay spent).
- On cancel (or any failure while in placement mode) the exact reserved amounts are added back via `AddScrap` / `AddCircuitry`.
- Because `Add` clamps at 999, the player may end at 999, but they will never permanently lose the reserved resources due to the cap.
- Only one placement reservation may be active at a time. Attempting to begin a new placement while one is already active must cancel the previous reservation (refunding it) before starting the new one, or simply reject the new request. The safer default is to cancel & refund the previous one.

---

## 6. User Interface Requirements

### 6.1 Inventory HUD
- Always visible during gameplay (or easily toggled).
- Displays current Scrap and Circuitry counts.
- Updates immediately when `OnInventoryChanged` fires.

### 6.2 Crafting Menu
- Opened / closed by a single configurable key (default recommendation: C).
- Lists every recipe registered with the CraftingManager.
- Each entry shows name, description, costs, and icon.
- Craft button is disabled when the player cannot afford the recipe or when a placement reservation is already active.
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
2. Walking over Scrap and Circuitry pickups increases the corresponding counters and the HUD updates. Collecting when already at 999 leaves the value at 999.
3. The crafting menu opens/closes with the configured key and lists all registered recipes.
4. Recipes the player cannot afford have a disabled craft button.
5. Crafting a WeaponPickup recipe spawns the prefab near the player and deducts the correct resources.
6. Crafting a Structure recipe enters placement mode, shows a non-interactive ghost, and allows placement with left click.
7. Cancelling placement restores the exact resources that were reserved.
8. Inventory values never go negative or exceed 999 under any tested sequence.
9. Only a single call to `DropLoot()` plus a conditional skip of the legacy drop has been added to the death path.

### 8.2 Required Edge-Case / Safety Tests
These must be explicitly verified:

- Negative spend costs are rejected; inventory is unchanged.
- Zero-cost recipes succeed (if all other validation passes).
- Negative or zero addition amounts are ignored.
- Inventory is correctly clamped at both 0 and 999.
- Duplicate calls to the death path on the same enemy do not produce extra drops beyond one evaluation of the LootTable.
- Missing player reference, missing resultPrefab, or missing StructurePlacer causes the craft to fail with **no** resource loss.
- Placement cancellation restores the exact reserved cost even if the player’s inventory was at or near 999 during placement.
- Beginning a second placement while one is active either refunds the first or rejects the second; resources are never double-spent or double-refunded.
- Invalid loot-table ranges (min > max, negative amounts, chance outside 0–1) are rejected or safely clamped with a warning.
- Ghost previews do not execute gameplay behaviour (no shooting, no triggers, no damage, etc.).
- Recipe duplicate IDs are rejected on registration.

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

This specification (v1.2) supersedes version 1.1 and all earlier informal descriptions.  
Changes to behaviour require an update to this document and a new version number.  
Implementation may begin only after this version has been reviewed and approved.
