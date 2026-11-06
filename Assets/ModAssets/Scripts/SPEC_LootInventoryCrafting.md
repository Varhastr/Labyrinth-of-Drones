# Specification: Loot Drops, Inventory & Crafting System

**Document Status:** Authoritative Specification  
**Version:** 1.1  
**Project:** Labyrinth of Drones  
**Target Engine:** Unity (FPS Microgame base)  
**Audience:** Junior Engineering  
**Last Updated:** 2026-11-06

This document is the single source of truth for the Loot, Inventory, and Crafting systems. All implementation must conform to this specification. The scaffolding code and any README checklists are supporting materials only and do not override this document.

---

## 1. Purpose & Scope

### 1.1 Purpose
Introduce a resource economy that turns defeated drones into progression. Players collect two primary resources (Scrap and Circuitry), store them in an inventory, and spend them to craft weapons, placeable structures, and utility items.

This system is the foundation for later features (base building, survivor support, longer-term progression).

### 1.2 In Scope (v1)
- Resource drops from enemies on death
- Simple inventory tracking two integer resources
- Crafting menu driven by ScriptableObject recipes
- Instantiation of weapon pickups
- Ghost-preview placement of structures
- Basic HUD display of current resources
- Refund of resources when placement is cancelled

### 1.3 Explicitly Out of Scope (v1)
- Item slots, equipment, or general-purpose item inventory
- Weight, stacking limits beyond the hard 999 cap, or inventory capacity
- Saving / loading of inventory state across sessions
- Multi-step crafting, crafting stations that must be powered, or research trees
- Complex building grid, rotation gizmos, or advanced validation (slope, overlapping volumes, NavMesh queries beyond basic raycast)
- Interaction between crafted objects and survivors
- Any modification or replacement of the existing enemy health / death system (only a single method call is allowed)

---

## 2. Resources

| Resource    | Type     | Max Value | Source                          | Notes |
|-------------|----------|-----------|---------------------------------|-------|
| Scrap       | Integer  | 999       | Enemy death (common)            | Primary crafting material |
| Circuitry   | Integer  | 999       | Enemy death (less common)       | Higher-tier crafting material |
| Health Pack | Instant  | N/A       | Enemy death or world spawn      | Does **not** enter inventory. Instant heal on pickup. Existing microgame health pickup may be reused. |

Resources never go below zero. Attempts to spend more than currently held must fail cleanly.

---

## 3. Inventory

### 3.1 Responsibilities
- Store current Scrap and Circuitry counts
- Provide atomic spend operations
- Notify listeners when values change
- Support a full reset (for new runs / death)

### 3.2 Interface (required)

```csharp
public static class Inventory
{
    public static int Scrap { get; }
    public static int Circuitry { get; }

    public static event System.Action OnInventoryChanged;

    public static void AddScrap(int amount);
    public static void AddCircuitry(int amount);
    public static bool TrySpend(int scrapCost, int circuitryCost); // returns false if insufficient funds
    public static void Reset();
}
```

### 3.3 Rules
- All mutations must clamp values to the range [0, 999].
- `TrySpend` must be atomic: either both resources are deducted or neither is.
- `OnInventoryChanged` must be raised after every successful mutation.
- Inventory is static and lives for the duration of a play session / run.

---

## 4. Loot System

### 4.1 Loot Table
A ScriptableObject that defines a list of possible drops.

Each entry contains:
- Reference to a pickup prefab
- Chance (0–1)
- Minimum and maximum amount to spawn when the roll succeeds

Multiple entries may succeed on a single death.

### 4.2 Drop Behaviour
When an enemy dies:
1. The `LootDropper` component on that enemy evaluates its assigned LootTable.
2. For every entry whose random roll succeeds, the specified number of pickup instances are spawned near the enemy’s position.
3. Each pickup receives a small upward + random horizontal impulse so the drops are visually readable.

### 4.3 Integration Constraint
`LootDropper.DropLoot()` must be called from the **existing** enemy death path (the same place the microgame already handles death, OnDie events, or equivalent).  
No new death system, no new health component, and no replacement of EnemyController death logic is permitted.

### 4.4 Pickup Behaviour
- Pickups are trigger colliders.
- On player contact they add their resource amount to the Inventory and destroy themselves.
- Pickups auto-destroy after a configurable lifetime (default 60 seconds) if not collected.

---

## 5. Crafting System

### 5.1 Recipe Definition
Recipes are ScriptableObjects. Required fields:

| Field            | Type              | Purpose |
|------------------|-------------------|---------|
| id               | string            | Unique stable key |
| displayName      | string            | Player-facing name |
| description      | string            | Player-facing text |
| icon             | Sprite            | UI icon |
| scrapCost        | int               | Cost in Scrap |
| circuitryCost    | int               | Cost in Circuitry |
| resultType       | CraftResultType   | Determines post-craft behaviour |
| resultPrefab     | GameObject        | Prefab to spawn or place |

`CraftResultType` values:
- `WeaponPickup` – spawn the prefab as a world pickup near the player
- `Structure` / `Utility` – enter placement mode
- `Consumable` – v1 may simply log or apply a trivial effect

### 5.2 Crafting Flow
1. Player opens the crafting UI.
2. UI displays all registered recipes with current affordability state.
3. Player selects a recipe and confirms.
4. System calls `Inventory.TrySpend`.
5. On success, behaviour is dispatched by `resultType`:
   - WeaponPickup → instantiate prefab in front of the player
   - Structure / Utility → hand control to the Structure Placer
   - Consumable → apply effect (minimal in v1)

### 5.3 Structure Placement
- Entering placement mode creates a ghost (non-colliding, non-physical) instance of the result prefab.
- Ghost follows a raycast from the main camera against a configurable layer mask, clamped to a maximum distance.
- Left mouse button places the real prefab at the hit point and exits placement mode.
- Right mouse button or Escape cancels placement and **refunds** the exact resource cost that was spent.
- Basic validity check is required (hit exists). More advanced validation (slope, overlap, NavMesh) is out of scope for v1 but the architecture must allow it to be added later.

---

## 6. User Interface Requirements

### 6.1 Inventory HUD
- Always visible during gameplay (or easily toggled).
- Displays current Scrap and Circuitry counts.
- Updates immediately when `OnInventoryChanged` fires.

### 6.2 Crafting Menu
- Opened / closed by a single configurable key (default recommendation: C).
- Lists every recipe known to the CraftingManager.
- Each entry shows name, description, costs, and icon.
- Craft button is disabled when the player cannot afford the recipe.
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

## 8. Acceptance Criteria

The system is considered complete for v1 when all of the following are true:

1. Killing an enemy that has a correctly configured LootDropper causes the expected pickups to appear and receive a visible impulse.
2. Walking over Scrap and Circuitry pickups increases the corresponding inventory counters and the HUD updates.
3. The crafting menu can be opened and closed with the configured key and correctly lists all registered recipes.
4. Recipes the player cannot afford have a disabled craft button.
5. Successfully crafting a WeaponPickup recipe spawns the prefab near the player and deducts the correct resources.
6. Successfully crafting a Structure recipe enters placement mode, shows a ghost, and allows placement with left click.
7. Cancelling placement (right click or Escape) restores the exact resources that were spent.
8. Inventory values never go negative or exceed 999.
9. No existing enemy death / health logic has been rewritten; only a single call to `DropLoot()` has been added.

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

This specification supersedes any earlier informal descriptions, checklists, or scaffolding comments.  
Changes to behaviour require an update to this document and a new version number.
