# Scene Setup (current test scene)

## Purpose
Snapshot of what's in the active scene and how it's wired, so either machine can reproduce or continue the setup. Update when the scene composition meaningfully changes.

**Scene:** `Assets/Scenes/SampleScene.unity` (to be renamed `GameScene` in Phase 0; not yet in Build Settings).

## Contents (root objects)

**Core:**
- `Main Camera` — Camera + AudioListener + URP camera data + **CinemachineBrain**
- `FirstPersonCamera` — CinemachineCamera + HardLockToTarget (position) + PanTilt (aim) + InputAxisController; the FP view rig
- `Directional Light`, `Global Volume` (URP post), `Floor` (mesh + MeshCollider)
- `Player` — `Player`, `PlayerInteract`, `PlayerCarry`, `PlayerPickupDrop`; children: head anchor/visual rig (Animator for `PlayerAnimator`)
- `GameInput` — `GameInput` component with `InputSystem_Actions` assigned
- `HUD` — Canvas + `PlayerInteractUI` (interact prompt container + TMP label)

**Gameplay test objects (layer 6 = interactable):**
- `ClearCounter`, `ClearCounter (1)` — BoxCollider + `ClearCounter`, child `counterTopPoint`
- `Tomato` (KitchenObject + Rigidbody + SphereCollider), `Cheese` (KitchenObject + Rigidbody + BoxCollider)

**Props (no gameplay components yet):**
- `Prop_Fridge_01` (untracked prefab at `Assets/_Assets/Prefabs/Counters/` — future ContainerCounter)
- `Bake` — pizza oven from the PizzA pack (future StoveCounter/oven)
- `Prop_KitchenTable_01`, `Prop_TrayHolder`, `Prop_KitchenCabinet_01`, `Kitchen_tabla_01`
- Environment shell: `Base_floor`, `Base_Wall_2`, `Base_Wal_1`, `Base_Pillar`

## Layers
- **6** — interactables/kitchen objects (counters, Tomato, Cheese). Not yet formally named/documented in project settings; Phase 0 formalizes the layer scheme + collision matrix.

## Notes
- `Assets/Screenshots/` receives MCP screenshot output — gitignore it.
- Uncommitted at time of writing: `SampleScene.unity` changes, `Prop_Fridge_01.prefab`, package manifest (MCP for Unity). Commit before switching machines.
