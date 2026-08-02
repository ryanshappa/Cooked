# Player Controller

## Purpose
First-person player movement and camera. The player walks around the kitchen with WASD (camera-relative), looks with the mouse via Cinemachine, and collides with the world without a CharacterController or Rigidbody — movement is manual with capsule casts.

## Files
- `Assets/_Assets/Scripts/Player.cs` — movement, collision, facing
- `Assets/_Assets/Scripts/PlayerAnimator.cs` — pushes `IsWalking`/`MoveX`/`MoveY` into an Animator (blend tree)
- Camera rig lives in the scene (see wiring), not in a script we own.

## How it works
- `Player.HandleMovement()` (called from `Update`) reads move input from `GameInput.ReadMove()`, converts it to world space relative to `cameraHolder`'s yaw (Y flattened), then attempts to move `moveSpeed * dt`.
- **Collision**: a `Physics.CapsuleCast` (from feet to `playerHeight`, radius `playerRadius`, against `collisionLayers`) blocks movement. If the full direction is blocked, it retries X-only, then Z-only — this gives wall sliding.
- Position is applied with `transform.position +=` (no Rigidbody, no CharacterController). There is **no gravity/jumping** — the player stays at floor height.
- The body slerps its `transform.forward` toward camera yaw (20f speed) so the character mesh faces where you look.
- Cursor is locked+hidden in `Start()`.
- `PlayerAnimator` reads `player.IsWalking()` and raw move input every frame and sets Animator params `IsWalking` (bool), `MoveX`, `MoveY` (floats).

## Scene/Inspector wiring
- `Player` GameObject: `Player`, `PlayerInteract`, `PlayerCarry`, `PlayerPickupDrop` components. `Player` needs: `cameraHolder` (head anchor transform), `input` (the GameInput object), tuned `moveSpeed` (5), `playerRadius` (0.7), `playerHeight` (2), `collisionLayers`.
- Camera rig (Cinemachine 3): scene `Main Camera` has `CinemachineBrain` + `AudioListener`. A separate `FirstPersonCamera` object has `CinemachineCamera` + `CinemachineHardLockToTarget` (position) + `CinemachinePanTilt` (aim) + `CinemachineInputAxisController` (reads Look input). It hard-locks to the player's head anchor.
- The child rig of Player carries the Animator (`PlayerAnimator` requires it on the same object).

## Known issues / TODO
- No gravity: fine on a flat kitchen floor, but stairs/ramps in the break room would need a grounding solution.
- Facing slerp uses camera forward even when standing still — intended (you face where you look).
- For multiplayer (Phase 4) this becomes owner-authoritative movement; keep all movement logic inside `Player` so the NGO conversion is one class.
- Head-bob / FP arms are Phase 10 decisions.
