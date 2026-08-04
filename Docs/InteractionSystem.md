# Interaction System

## Purpose
First-person "look at a thing, press E" interaction: one camera-forward cast per frame decides what the reticle is on, one contextual action runs on Interact (pick up / place / use / drop), and the HUD prompt always describes exactly what pressing E will do — prompt and action can never disagree because they read the same target.

## Files
- `Assets/_Assets/Scripts/PlayerInteract.cs` — the unified system (targeting + all actions)
- `Assets/_Assets/Scripts/IInteractable.cs` — interface for stations/props: `Interact(Transform)`, `GetInteractText()`, `GetTransform()`
- `Assets/_Assets/Scripts/PlayerCarry.cs` — the "hands": implements `IKitchenObjectParent`, owns the camera-following hold point
- `Assets/_Assets/Scripts/PlayerInteractUI.cs` — HUD prompt

> History: this replaced the original `PlayerInteract` + `PlayerPickupDrop` pair (two parallel raycasts, two Interact bindings, different ranges and hit resolution — the source of "prompt says one thing, E does another" and "place only works dead-center"). `PlayerPickupDrop` is deleted.

## How it works
Each frame `PlayerInteract.ResolveTarget()`:
1. Casts a **precise ray** from the camera (`maxDistance` 2.5, `interactMask`, triggers included); if it misses, retries as a small **spherecast** (`assistRadius` 0.1) for forgiveness on thin objects. Precise-first keeps the prompt honest to the reticle.
2. All component lookups use `GetComponentInParent`, so hitting any child collider of a counter/object works.
3. Decides one `InteractAction` for the frame:
   - **Holding something** → hit an `IKitchenObjectParent` surface (not our own hands): **Place** if it's empty, *no action* if occupied (deliberately: never toss into a counter face). Hit anything else / nothing → **Drop** (forward toss at `dropTossSpeed`).
   - **Empty-handed** → hit a `KitchenObject` not held by a player: **Pickup**. Else hit an `IInteractable`: **Use**.
4. On `GameInput.IsInteractPressed()` the action executes (`SetParent(carry)` / `SetParent(surface)` / `Interact()` / `DropWithPhysics`).

`HasPrompt(out text)` exposes a contextual prompt string ("Pick up Tomato", "Place Cheese", …). **The HUD no longer renders it** — text prompts were removed by design (Aug 2026): the plain crosshair is the only aim feedback; physical affordances carry the meaning. The API and `PlayerInteractUI.cs` are kept for the break-room practice stations (Phase 7), where wordy tutorial prompts are welcome.

**Blue hover outline (`TargetHighlighter` + `YesChef/Outline` shader):** whatever the system currently targets for **Pickup** or **Use** (items, fridge doors, grab points — not bare counters) gets a world-space inverted-hull outline (`OutlineBlue.mat`). It tracks the target, so it disappears the instant you pick the item up. Cooking Simulator-style.

**Pierce-through targeting:** the empty-hand ray walks all hits front-to-back and pierces through *empty* surfaces (so a knife lying inside a table slot's raised collider is still reachable), but stops at dedicated interactables (a closed fridge door blocks reaching the shelf items behind it). Occupied surfaces still forward to their item.

`PlayerCarry`: a `DynamicHoldPoint` transform repositioned every frame at camera + `holdOffset` — set to **(0, 0, 0.7)**, i.e. dead-center on the reticle. Held objects glue to it with zero lag, centered by collider bounds (see KitchenObjectSystem.md).

## Scene/Inspector wiring
- `Player` GameObject: `Player`, `PlayerInteract`, `PlayerCarry` (PlayerPickupDrop removed). `PlayerInteract` needs `interactMask` (layer 6), `input` (GameInput), and optionally `cameraTransform` (falls back to `Camera.main`). `PlayerCarry` needs the FP camera transform.
- `HUD` canvas: just the `Crosshair` (prompt UI removed from the scene).
- Interactables live on **layer 6**; components on the root, colliders anywhere below.

## Known issues / TODO
- Feel tuning pending player testing: `assistRadius`, `maxDistance`, `dropTossSpeed`, hold offset smoothing.
- The Main Camera's culling mask is hand-picked (excludes `PlayerBody`); **any new layer that must render has to be added to it explicitly** — the `Held` layer was invisible until added (mask 119 → 375).
- Highlight/outline on the targeted object: Phase 1.
- Phase 2 will extend the action set for held tools (knife/ladle) — the enum + resolve structure is designed to grow.
