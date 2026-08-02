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
| `FridgeCounter` (fridge) | toggles the right door (eased hinge swing, `openAngle` 100°) | **no** | ingredients inside are `IngredientGrabPoint` display props |

**`IngredientGrabPoint`** (`IngredientGrabPoint.cs`): a display prop on a fridge shelf. Use with empty hands → `KitchenObject.Spawn` of its SO into your grip; the prop never depletes (it's "stock"). Gated on `fridge.IsOpen`, though the closed door's collider blocks the ray anyway. Current stock: 2× Cheese (upper shelf), 2× Meat (raw steak, shelf below).

**Fridge internals** (`Prefabs/Counters/Prop_Fridge_01.prefab`): the Pandazole door meshes have their origin at the hinge edge, so the right door (`_p02`) is driven directly by yaw — no pivot re-parenting. Body collider is split in two (left half full-depth, right half rear-only) so the right-front opening is raycast-reachable when the door is open; the door has its own thin collider. Clicking anywhere on the body/door toggles the door; clicking a shelf item takes it.

Planned next (see CLAUDE.md Phase 1): TrashCounter, PlatesCounter, CuttingCounter (bar v1), StoveCounter (bar v1), DeliveryCounter.

## Scene/Inspector wiring
- Counter prefab: root = collider(s) + counter component on layer **6 Interactable** (all children too); child `CounterTopPoint` anchor assigned on placeable counters.
- `ContainerCounter` needs its `kitchenObjectSO` set; `counterTopPoint` stays null (that's what blocks placement).
- The fridge prefab has ContainerCounter on the root with Cheese assigned; scene instance inherits.

## Known issues / TODO
- Fridge door has no sound yet; left door is static (could become a second compartment later).
- Aiming at the fridge *interior body* (between items) with empty hands toggles the door closed — convenient but possibly annoying; revisit after playtesting.
- Selected-counter highlight visual still pending (Phase 1 item).
- One slot per counter stays the rule until plates (which get their own contents model).
