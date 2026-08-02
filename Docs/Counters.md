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
| `ContainerCounter` (fridge) | spawns its `kitchenObjectSO` into the player's hands via `KitchenObject.Spawn` | **no** | `OnPlayerGrabbedObject` C# event for door-anim/sound hooks; fridge currently dispenses Cheese |

Planned next (see CLAUDE.md Phase 1): TrashCounter, PlatesCounter, CuttingCounter (bar v1), StoveCounter (bar v1), DeliveryCounter.

## Scene/Inspector wiring
- Counter prefab: root = collider(s) + counter component on layer **6 Interactable** (all children too); child `CounterTopPoint` anchor assigned on placeable counters.
- `ContainerCounter` needs its `kitchenObjectSO` set; `counterTopPoint` stays null (that's what blocks placement).
- The fridge prefab has ContainerCounter on the root with Cheese assigned; scene instance inherits.

## Known issues / TODO
- Fridge has no door animation/sound yet — subscribe to `OnPlayerGrabbedObject` when we add it.
- Selected-counter highlight visual still pending (Phase 1 item).
- One slot per counter stays the rule until plates (which get their own contents model).
