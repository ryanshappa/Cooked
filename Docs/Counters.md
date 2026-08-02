# Counters & Stations

## Purpose
Every kitchen station derives from one base: a **counter** is a single-slot holder for a kitchen object that can also react to the player's Use action. Stations differ only in what they do on Use and whether they accept placed objects.

## Files
- `Assets/_Assets/Scripts/BaseCounter.cs` — abstract base (`IKitchenObjectParent` + `IInteractable`)
- `Assets/_Assets/Scripts/ClearCounter.cs` — plain work surface (no behavior of its own)
- `Assets/_Assets/Scripts/ContainerCounter.cs` — ingredient source (the fridge)
- Prefabs: `Prefabs/Counters/ClearCounter.prefab`, `Prefabs/Counters/Prop_Fridge_01.prefab`

## How it works
`BaseCounter` provides:
- The single slot (`SetKitchenObject`/`GetKitchenObject`/…) and the `counterTopPoint` anchor where placed objects snap.
- `virtual Interact(Transform interactor)` — default no-op; stations override.
- `virtual CanAcceptKitchenObject()` — default `counterTopPoint != null`; the unified interaction system checks this (plus emptiness) before allowing Place. This is how a station opts out of being a shelf.

Station catalog (grows through Phase 1):

| Station | Use (E, empty-handed) | Accepts placement? | Notes |
|---|---|---|---|
| `ClearCounter` | nothing | yes | pure surface |
| `ContainerCounter` | spawns its `kitchenObjectSO` into the player's hands | **no** | generic instant dispenser — currently unused (kept for crates/simple sources); `OnPlayerGrabbedObject` event |
| `FridgeCounter` (fridge body) | nothing (doors handle interaction) | **no** | body is just a blocker/anchor for the doors + stock |
| `FridgeDoor` (per door) | toggles that door (eased hinge swing; right +100°, left −100°) | n/a | to close, click the open door again — its collider swings with it |

**`IngredientGrabPoint`** (`IngredientGrabPoint.cs`): a display prop on a fridge shelf. Use with empty hands → `KitchenObject.Spawn` of its SO into your grip; the prop never depletes (it's "stock"). Gated on its compartment's `door.IsOpen` (the closed door's collider also physically blocks the ray). Current stock — right compartment: 2× Cheese (top shelf), 2× Meat (raw steak, shelf below); left compartment: 2× Tomato (top shelf).

**Fridge internals** (`Prefabs/Counters/Prop_Fridge_01.prefab`): the Pandazole door meshes have their origin at the hinge edge, so each door is driven directly by yaw — no pivot re-parenting. Both body-half colliders are rear-only slabs, leaving the front openings raycast-reachable when a door is open; each door has its own thin collider. Shelf tops sit at local y **0.48 / 0.83 / 1.18 / 1.54** (extracted from mesh vertices) — place stock at exactly these heights.

**`TrashBin` (physical dumpster)** (`Prefabs/Counters/TrashBin.prefab`): deliberately **not** a counter and has **no script** — it's the Code Monkey trash-bin visual with an open-top compound collider (floor + 4 walls). Toss items in (aim into it, E) and they physically fall in, pile up, and stay — Schedule 1 dumpster-style; you can fish things back out. There is no destroy-on-contact by design. If piles ever become a perf/gameplay problem, add a slow despawn or a "collected overnight" rule (revisit at Phase 3 scoring). Trash SFX for later: `_Assets/Sounds/SFX/SFX_trash01/02.wav`.

**`KitchenTable` (multi-slot surface)** (`Prefabs/Counters/KitchenTable.prefab`): the pattern for surfaces with several snap points — root wraps the pack visual (nested prefab), plus one child **slot** per snap position (`Slot_L`, `Slot_R`), each being its own `ClearCounter` with a half-tabletop collider (raised above the surface so aim hits it) and its own `CounterTopPoint`. Aiming at a half places into that half; no interaction-code changes needed since each slot is just a counter. ⚠️ Snap-height lesson: set `CounterTopPoint` from the **actual surface plateau in the mesh vertices** (table: y 0.97), not renderer-bounds max — this model's raised back rail made bounds-max items hover 16cm.

Planned next (see CLAUDE.md Phase 1): TrashCounter, PlatesCounter, CuttingCounter (bar v1), StoveCounter (bar v1), DeliveryCounter.

## Scene/Inspector wiring
- Counter prefab: root = collider(s) + counter component on layer **6 Interactable** (all children too); child `CounterTopPoint` anchor assigned on placeable counters.
- `ContainerCounter` needs its `kitchenObjectSO` set; `counterTopPoint` stays null (that's what blocks placement).
- The fridge prefab has ContainerCounter on the root with Cheese assigned; scene instance inherits.

## Known issues / TODO
- Fridge doors have no sound yet.
- Both compartments now open independently; clicking the fridge body does nothing (no accidental door toggles while reaching for items).
- Selected-counter highlight visual still pending (Phase 1 item).
- One slot per counter stays the rule until plates (which get their own contents model).
