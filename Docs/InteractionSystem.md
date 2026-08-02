# Interaction System

## Purpose
First-person "look at a thing, press E" interaction: a camera-forward raycast decides what the player is targeting, a UI prompt shows what pressing Interact will do, and pressing it either interacts with a station or picks up / places / drops a kitchen object.

## Files
- `Assets/_Assets/Scripts/IInteractable.cs` — interface: `Interact(Transform interactor)`, `GetInteractText()`, `GetTransform()`
- `Assets/_Assets/Scripts/PlayerInteract.cs` — targeting raycast + generic interact
- `Assets/_Assets/Scripts/PlayerPickupDrop.cs` — pickup/place/drop logic (parallel path, see issues)
- `Assets/_Assets/Scripts/PlayerCarry.cs` — the "hands": implements `IKitchenObjectParent`, owns the hold point
- `Assets/_Assets/Scripts/PlayerInteractUI.cs` — HUD prompt

## How it works
**Targeting (`PlayerInteract`)**: every frame, spherecast (r=0.12, forgiving for thin objects) then raycast fallback from `Camera.main` forward, `maxDistance` 2.5, against `interactMask`, triggers included. Resolves `IInteractable` via `GetComponentInParent` so child colliders work. On Interact press it calls `current.Interact(transform)` — but **skips `KitchenObject` targets**, deferring those to `PlayerPickupDrop`.

**Pickup/drop (`PlayerPickupDrop`)**: on Interact press (own action binding):
- Empty-handed: raycast (camera forward, 2.2m, `interactMask`); if it hits a `KitchenObject` not already held by a player → `ko.SetParent(carry)`.
- Holding something: if looking at an `IKitchenObjectParent` surface with free space → place (`held.SetParent(surface)`); otherwise **drop with physics** (`held.DropWithPhysics(cameraForward * 1.5f, 0)`) — a forward toss.

**Carry (`PlayerCarry`)**: creates a world-space `DynamicHoldPoint` transform in `Awake` and repositions it every frame at `camera position + camera-rotated holdOffset` (0, -0.2, 0.5). Held objects follow it (see KitchenObjectSystem.md for the follow/physics details).

**Prompt (`PlayerInteractUI`)**: polls `PlayerInteract.GetInteractableObject()` each frame; shows/hides a HUD container and sets its TMP label to `GetInteractText()`.

## Scene/Inspector wiring
- On `Player`: `PlayerInteract` (needs `interactMask`, `GameInput` ref), `PlayerPickupDrop` (needs FP camera transform, `interactMask`, the `InputSystem_Actions` asset), `PlayerCarry` (needs FP camera transform).
- `HUD` canvas: `PlayerInteractUI` with refs to `PlayerInteract`, the prompt container GameObject, and its `TextMeshProUGUI`.
- Interactables (counters, kitchen objects) are on **layer 6**, which the interact masks target. Colliders may be on children; the parent holds the component.

## Known issues / TODO (Phase 0 target)
- **Two parallel interaction paths.** `PlayerInteract` and `PlayerPickupDrop` both raycast every frame and both bind Interact, coordinating only via the "skip KitchenObject" special case and slightly different ranges (2.5 vs 2.2). Merge into one system: single targeting pass → context decision (use station / pick up / place / drop) → single Interact consumer through `GameInput`.
- `PlayerPickupDrop`'s place-check uses `TryGetComponent` on the hit collider (no parent search), so a surface whose collider is on a child won't be detected — inconsistent with targeting. Fix in the merge.
- Prompt text is static per object ("Pick up"); after the merge it should reflect the contextual action ("Place", "Take plate", …).
- Highlight/outline on the targeted object: planned Phase 1.
