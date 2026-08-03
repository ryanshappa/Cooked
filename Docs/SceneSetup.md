# Scene Setup (current test scene)

## Purpose
Snapshot of what's in the active scene and how it's wired, so either machine can reproduce or continue the setup. Update when the scene composition meaningfully changes.

**Scene:** `Assets/Scenes/GameScene.unity` (renamed from SampleScene, GUID preserved; not yet in Build Settings).

## Contents (root objects)

**Core:**
- `Main Camera` — Camera + AudioListener + URP camera data + **CinemachineBrain**
- `FirstPersonCamera` — CinemachineCamera + HardLockToTarget (position) + PanTilt (aim) + InputAxisController; the FP view rig
- `Directional Light`, `Global Volume` (URP post), `Floor` (mesh + MeshCollider)
- `Player` — `Player`, `PlayerInteract`, `PlayerCarry`; children: head anchor/visual rig (Animator for `PlayerAnimator`)
- `GameInput` — `GameInput` component with `InputSystem_Actions` assigned
- `HUD` — Canvas with `Crosshair` only (text prompts removed by design; `PlayerInteractUI.cs` kept on disk for break-room tutorial use)

**Draft kitchen (Aug 2026)** — a ~13×8m perimeter rectangle, stations facing inward, walk-in entrance gap mid-south. All counters normalized to surface ≈ y 1.0.

| Edge | Contents (west→east / north→south) |
|---|---|
| North (z=2.6, rot 180) | pizza oven `Bake` (NW corner, mouth facing in) · Clear · Stove ×2 · Cutting ×2 · Plates · Clear |
| West (x=−6.4, rot 90) | Clear · **Delivery** · Clear |
| East (x=6.4, rot 270) | Clear · **Fridge** · Clear |
| South (z=−4.6, rot 0) | Clear ×2 · KitchenTable · **entrance gap** · KitchenTable · Clear ×2 |
| Inside | `TrashBin` at SE corner (5.3, −3.4); Player spawns center (0, −1); loose Tomato/Cheese on the two north Clear counters |

Counter fronts face +z at rot 0 (handles/knobs side). ⚠️ Scene instances must come from `_Assets/Prefabs/Counters/` — identical-looking *Pandazole pack* prefab instances were silently in the scene once (fridge AND table); pack prefabs get none of our components. ⚠️ Editor-scripting note: `GameObject.Find` matches same-named *children* (Tomato/Bake/wall roots all have same-named kids) — always resolve scene roots via `scene.GetRootGameObjects()`.

**Parked at +25/+25 (not deleted):** old test-scene deco — `Base_Wall_2`, `Base_Wal_1`, `Base_Pillar`, `Kitchen_tabla_01`.

## Layers
- **3 `PlayerBody`** — (pre-existing, currently unused by scripts)
- **6 `Interactable`** — counters + kitchen objects; `PlayerInteract.interactMask` targets exactly this
- **8 `Held`** — assigned at runtime to whatever the player is carrying; excluded from `Player.collisionLayers` and from the interact mask so carried items never block movement or the reticle

## Notes
- `Assets/Screenshots/` receives MCP screenshot output — gitignore it.
- Uncommitted at time of writing: `SampleScene.unity` changes, `Prop_Fridge_01.prefab`, package manifest (MCP for Unity). Commit before switching machines.
