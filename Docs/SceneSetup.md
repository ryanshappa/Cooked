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

**Gameplay test objects (layer 6 = interactable):**
- `ClearCounter`, `ClearCounter (1)` — BoxCollider + `ClearCounter`, child `counterTopPoint`
- `Tomato` (KitchenObject + Rigidbody + SphereCollider), `Cheese` (KitchenObject + Rigidbody + BoxCollider)

**Props:**
- `Prop_Fridge_01` — **FridgeCounter** body + two independent **FridgeDoor**s; shelf stock: Cheese+Meat (right), Tomato (left), layer 6. ⚠️ Scene instances must come from `_Assets/Prefabs/Counters/` — identical-looking *Pandazole pack* prefab instances were silently in the scene (fridge AND table); pack prefabs get none of our components.
- `KitchenTable` — two-slot placing surface (`Slot_L`/`Slot_R`, each a ClearCounter), replaces the old pack-prefab `Prop_KitchenTable_01` instance.
- `Bake` — pizza oven from the PizzA pack (future StoveCounter/oven)
- `Prop_KitchenTable_01`, `Prop_TrayHolder`, `Prop_KitchenCabinet_01`, `Kitchen_tabla_01`
- Environment shell: `Base_floor`, `Base_Wall_2`, `Base_Wal_1`, `Base_Pillar`

## Layers
- **3 `PlayerBody`** — (pre-existing, currently unused by scripts)
- **6 `Interactable`** — counters + kitchen objects; `PlayerInteract.interactMask` targets exactly this
- **8 `Held`** — assigned at runtime to whatever the player is carrying; excluded from `Player.collisionLayers` and from the interact mask so carried items never block movement or the reticle

## Notes
- `Assets/Screenshots/` receives MCP screenshot output — gitignore it.
- Uncommitted at time of writing: `SampleScene.unity` changes, `Prop_Fridge_01.prefab`, package manifest (MCP for Unity). Commit before switching machines.
