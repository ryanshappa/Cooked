# Kitchen Object System

## Purpose
Kitchen objects (ingredients, later plates/tools) are physical items that can exist in three states: **loose in the world** (full physics), **held by a player** (following the camera hold point, no collisions), or **placed on a surface** (parented to a counter's anchor point). Any holder — player hands or counter — is an `IKitchenObjectParent`.

## Files
- `Assets/_Assets/Scripts/KitchenObject.cs` — the item itself
- `Assets/_Assets/Scripts/IKitchenObjectParent.cs` — interface: follow transform + get/set/clear/has kitchen object
- `Assets/_Assets/Scripts/KitchenObjectSO.cs` — data: `prefab` (Transform), `sprite`, `objectName`
- `Assets/_Assets/Scripts/ClearCounter.cs` — simplest surface: one anchor (`counterTopPoint`), one slot
- Assets: `ScriptableObjects/KitchenObjectSO/{Tomato,Cheese}.asset` → `Prefabs/KitchenObjects/{Tomato,Cheese}.prefab` (each: root = KitchenObject + Rigidbody + collider sized to the visual; visual child); `Prefabs/Counters/ClearCounter.prefab`. `CheeseBlock_Visual.prefab` is visual-only (used inside Cheese.prefab).

## How it works
`KitchenObject` caches its `Rigidbody` and all child colliders in `Awake`. State transitions happen in `SetParent(IKitchenObjectParent)`:

| State | Physics | Transform |
|---|---|---|
| Held by `PlayerCarry` | colliders **off**, rb kinematic (velocities zeroed first) | unparented; `Update()` copies the hold-point position/rotation each frame for zero-lag follow |
| On a counter/surface | colliders **on** (needed so you can target it), rb dynamic but `useGravity=false` | parented to the surface's follow transform, local pos/rot zeroed |
| Loose (dropped) | colliders on, rb dynamic + gravity | unparented |

- `SetParent` also unlinks the previous parent (`ClearKitchenObject`) and links the new one (`SetKitchenObject`), keeping the parent's single slot consistent.
- `DropWithPhysics(linearVel, angularVel)` — used by the drop action: clears the parent, re-enables physics, applies a throw velocity.
- Ordering matters and was bug-prone: velocities must be zeroed *before* setting `isKinematic=true`, and `isKinematic=false` must be set *before* assigning velocities. Don't "simplify" this.
- Pickup/place goes through the unified `PlayerInteract` (see InteractionSystem.md); `KitchenObject` no longer implements `IInteractable`.
- **Collider hygiene rule** (learned the hard way): the interaction collider must tightly match the *visual* bounds — mismatched colliders make the reticle/prompt feel broken. Colliders live in the prefab, never as scene-instance additions. (Aug 2026: Tomato's collider was scene-only + offset below the visual; "Cheese" was a hacked Tomato-prefab instance — both fixed, Cheese promoted to its own prefab.)

`KitchenObjectSO` is minimal data for now; it becomes the currency of recipes (`CuttingRecipeSO`, `RecipeSO`) in Phases 1–3. Planned Phase 0 addition: static `KitchenObject.Spawn(so, parent)` / `Destroy` helpers so all instantiation is centralized (prereq for clean NGO spawning in Phase 4).

## Scene/Inspector wiring
- Kitchen object prefab: root has `KitchenObject` (with its SO assigned), `Rigidbody`, collider(s); visual mesh as child. Layer 6 so interaction raycasts hit it.
- `ClearCounter` prefab: `BoxCollider` + `ClearCounter`, child `counterTopPoint` anchor assigned; layer 6.
- Current test scene has loose `Tomato` and `Cheese` instances and two ClearCounters.

## Known issues / TODO
- One slot per parent — plates (multi-ingredient) will need their own contents model (Phase 1).
- Counter-placed objects are dynamic-no-gravity; a shove could theoretically move them — revisit if it shows up in play.
- `KitchenObjectSO.prefab` is typed `Transform` (Code Monkey style); fine, but Spawn helpers should hide that quirk.
- ClearCounter will become a `BaseCounter` subclass in Phase 1.
