# Input System

## Purpose
All player input flows through Unity's Input System (`InputSystem_Actions.inputactions`) wrapped by a `GameInput` singleton so gameplay code never touches devices directly.

## Files
- `Assets/InputSystem_Actions.inputactions` — the action asset (maps: `Player`, `UI`)
- `Assets/_Assets/Scripts/GameInput.cs` — singleton wrapper

## How it works
- `GameInput` is a scene singleton (`Instance`, `DontDestroyOnLoad`). On `Awake` it resolves the `Player` and `UI` action maps and caches actions: `Move`, `Look`, `Jump`, `Sprint`, `Interact` (Jump/Sprint/Interact are optional lookups).
- `OnEnable` enables the asset with the `Player` map on and `UI` map off. `SetPlayerInputActive(bool)` / `SetUIInputActive(bool)` switch maps (this is how pause menus should take over input later).
- Convenience readers: `ReadMove()`, `ReadLook()`, `IsInteractPressed()`, etc.
- Cinemachine's `CinemachineInputAxisController` reads Look on its own (bound to the same asset), not through `GameInput`.

## Scene/Inspector wiring
- A `GameInput` GameObject in the scene with the component; `actionsAsset` must reference `InputSystem_Actions`.
- Consumers (`Player`, `PlayerInteract`) hold a serialized `GameInput` reference (dragged in Inspector) rather than using `Instance` — either works; be consistent when adding new consumers.

## Known issues / TODO
- **`PlayerPickupDrop` bypasses `GameInput`** — it holds its own `InputActionAsset` reference and resolves `Player/Interact` itself. This means two systems independently bind Interact. Fix as part of the Phase 0 interaction merge: everything reads through `GameInput`.
- No rebinding UI yet (Phase 11 settings).
- `Jump`/`Sprint` are wired but unused by design (no jumping in kitchens).
