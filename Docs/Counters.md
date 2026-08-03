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
| `PlatesCounter` | nothing (the display plate on top is the grab point) | **no** | grab the plate stack to receive a fresh `Plate` |
| `CuttingCounter` | — (E places/picks up; **LMB chops** via `IWorkStation`) | only items with a **CuttingRecipeSO** | after `cutsRequired` chops the item becomes the recipe output; squash-scale placeholder feedback |
| `StoveCounter` | — (cooking is automatic) | only items with a **CookingRecipeSO** | timer per occupant; chained recipes give Cooked→Burned; `GetCookProgress()` for future UI/audio |

**Recipe-based acceptance (the "only certain items" rule):** `CanAcceptKitchenObject(KitchenObject incoming)` now takes the incoming item. A station accepts an item **iff it has a recipe for it** — no separate category tags needed. So vegetables can't go on the stove (no cooking recipe), meat can't go on the cutting board (no cutting recipe), and plates go on neither (no recipes at all). `PlateHolder` additionally refuses other plates. Recipes live in `ScriptableObjects/Recipes/`: `Cut_Tomato` (5 cuts), `Cut_Cheese` (4), `Cook_Meat` (8s), `Burn_CookedSteak` (12s more).

**Work input (`IWorkStation`):** the Attack action (LMB) is the "do work at a station" input, resolved from the same aim ray as everything else. Today it's chop-on-click; in Phase 2 the physics minigames take over this input. E remains strictly pickup/place.
| `FridgeDoor` (per door) | toggles that door (eased hinge swing; right +100°, left −100°) | n/a | to close, click the open door again — its collider swings with it |

**`IngredientGrabPoint`** (`IngredientGrabPoint.cs`): a display prop on a fridge shelf. Use with empty hands → `KitchenObject.Spawn` of its SO into your grip; the prop never depletes (it's "stock"). Gated on its compartment's `door.IsOpen` (the closed door's collider also physically blocks the ray). Current stock — right compartment: 2× Cheese (top shelf), 2× Meat (raw steak, shelf below); left compartment: 2× Tomato (top shelf).

**Fridge internals** (`Prefabs/Counters/Prop_Fridge_01.prefab`): the Pandazole door meshes have their origin at the hinge edge, so each door is driven directly by yaw — no pivot re-parenting. Both body-half colliders are rear-only slabs, leaving the front openings raycast-reachable when a door is open; each door has its own thin collider. Shelf tops sit at local y **0.48 / 0.83 / 1.18 / 1.54** (extracted from mesh vertices) — place stock at exactly these heights.

**`TrashBin` (physical dumpster)** (`Prefabs/Counters/TrashBin.prefab`): deliberately **not** a counter and has **no script** — it's the Code Monkey trash-bin visual with an open-top compound collider (floor + 4 walls). Toss items in (aim into it, E) and they physically fall in, pile up, and stay — Schedule 1 dumpster-style; you can fish things back out. There is no destroy-on-contact by design. If piles ever become a perf/gameplay problem, add a slow despawn or a "collected overnight" rule (revisit at Phase 3 scoring). Interior colliders: floor top at local y **0.33** (the bin's real interior bottom) + 4 walls tilted ~18° following the bin's taper (half-width 0.32 at floor → 0.60 at rim) — straight walls let items poke out of the tapered lower body. Trash SFX for later: `_Assets/Sounds/SFX/SFX_trash01/02.wav`.

**`Plate` + `PlateHolder`** (`Prefabs/KitchenObjects/Plate.prefab`): the plate is a normal KitchenObject *and* carries a `PlateHolder` (IKitchenObjectParent, one slot, `PlateTopPoint` anchor) — so a plate sitting on a counter accepts food placed onto it. This is the seed of dish assembly. Pack visual scaled 0.28× (the Kitchen Chaos plate is 1m wide!).

**Interaction forwarding** (in `PlayerInteract`, added for the table-slot shadow bug): empty-handed, hitting an **occupied surface counts as aiming at its item** (slot colliders enclose placed items and would otherwise eat the ray) → you pick the item up. Holding food, hitting an occupied surface checks whether the **occupant is itself an empty accepting holder** (a plate) → you place onto the plate. Aiming at food on a plate picks the food off the plate; aiming at the plate's rim picks up the plate (any food on it rides along — collider toggles use live child queries for exactly this).

**`KitchenTable` (multi-slot surface)** (`Prefabs/Counters/KitchenTable.prefab`): the pattern for surfaces with several snap points — root wraps the pack visual (nested prefab), plus one child **slot** per snap position (`Slot_L`, `Slot_R`), each being its own `ClearCounter` with a half-tabletop collider (raised above the surface so aim hits it) and its own `CounterTopPoint`. Aiming at a half places into that half; no interaction-code changes needed since each slot is just a counter. ⚠️ Snap-height lesson: set `CounterTopPoint` from the **actual surface plateau in the mesh vertices**, not renderer-bounds max — and make sure it's the slab's *top* face, not its underside (this table: rail top 1.13, slab underside 0.97, true top **1.03**). Bounds-max made items hover; the underside made them sink.

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
