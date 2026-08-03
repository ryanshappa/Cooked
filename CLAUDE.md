# Yes Chef — Project Guide for Claude

## What this game is

**Yes Chef** is a 1–4 player co-op cooking game (Overcooked-like) built in **Unity 6** with four twists that define every design decision:

1. **First-person perspective** — not top-down. Players see the kitchen through their chef's eyes.
2. **Physics-based cooking minigames** — no "hold E to fill a bar." Players manually sear and flip steaks, chop vegetables for sides, brush on marinades and butter, and plate dishes with real physics interactions (inspiration: Schedule 1's hands-on tasks).
3. **Proximity voice chat** — positional, distance-attenuated voice plus diegetic kitchen noise (timers beeping, oil sizzling) so communication is itself a gameplay challenge.
4. **Randomized kitchen layouts** — the kitchen is procedurally arranged each match so players can't memorize routes.

Supporting features: a **physical lobby** (restaurant break room with practice stations, ready-up, host-starts-game), **order scaling** by player count, **money earned per completed order** spent on cosmetic accessories (aprons, hats), and multiple **modes** (Party vs-the-clock, Endless survival, VS team competition).

**Setting: a steakhouse.** The restaurant serves steak dinners — steaks (the Meat ingredient), sides, plated meals. Recipes are TBD (designed in Phase 3/10); until then, steak-centric examples are the default when writing content or docs. Everything is physical — items, counters, and eventually the chefs themselves: colliding players will be able to knock held food out of each other's hands (dropped food becomes dirty/wasted).

Reference material: Code Monkey's Kitchen Chaos free course + its multiplayer follow-up (https://unitycodemonkey.com/kitchenchaosmultiplayercourse.php). We borrow its architecture patterns (KitchenObject/IKitchenObjectParent, ScriptableObject recipes, NGO server-auth multiplayer) but adapt everything to first-person + physics.

## Tech stack

| Thing | Choice |
|---|---|
| Engine | Unity **6000.1.15f1**, upgrading to **Unity 6.5 (6000.5.x)** as the first Phase 0 task — see `Docs/PLAN.md` Decision 1. Keep Mac + PC on the exact same version; version bumps are their own commit. |
| Render pipeline | URP 17.1 |
| Input | Input System 1.14.2 (`InputSystem_Actions.inputactions`, wrapped by `GameInput`) |
| Camera | Cinemachine 3.1.4 — `Main Camera` has a `CinemachineBrain`; `FirstPersonCamera` (CinemachineCamera + PanTilt + HardLockToTarget + InputAxisController) does the FP look |
| Netcode | **Netcode for GameObjects 2.4.4** (already installed) |
| Sessions/voice (planned) | UGS **Multiplayer Services SDK (Sessions API)** — Auth + Lobby + Relay in one package — plus **Vivox** for positional proximity voice. Facepunch Steamworks comes later as an alternate transport/platform layer at Steam release. All connectivity code lives in one `MultiplayerBootstrap` class. Full reasoning: `Docs/PLAN.md` Decisions 2–3. |
| Editor automation | **MCP for Unity is set up and connected** — use it (with the `unity-mcp-skill`) for scene/prefab/component/script work instead of hand-editing YAML. Take screenshots to verify visual changes. |
| Asset creation | Blender MCP is also connected for modeling help. Art packs on hand: Pandazole Ultimate Pack, "PizzA" pack (pizza oven, kitchen props). |

## Repo layout & conventions

- All original work lives in `Assets/_Assets/` (Scripts, Prefabs, ScriptableObjects, Materials…). Third-party packs stay in their own top-level folders. The active scene is currently `Assets/Scenes/SampleScene.unity`.
- Scripts: one class per file, PascalCase, no namespace currently (fine for now). Interfaces prefixed `I`.
- ScriptableObjects: `<Thing>SO` classes, assets in `Assets/_Assets/ScriptableObjects/<Type>/`.
- Layer 6 is the interactable/kitchen-object layer (counters, Tomato, Cheese live there). Layer usage is not yet formalized — see Phase 0.
- **Docs rule: every gameplay system gets a markdown file in `/Docs`. When you create or meaningfully change a system, update its doc in the same commit.** The dev switches between a Mac and a PC — `/Docs` + this file are the shared brain; never assume undocumented context survives the machine switch.
- Commit often; scene files (`.unity`) and prefabs are merge-hostile, so avoid leaving them dirty when switching machines.

## Current state (what already works)

First-person controller with capsule-cast collision (`Player.cs`), Cinemachine FP camera rig, input wrapper singleton (`GameInput.cs`), camera-raycast interaction with UI prompt (`PlayerInteract`, `IInteractable`, `PlayerInteractUI`), physics pickup/carry/drop of kitchen objects (`PlayerPickupDrop`, `PlayerCarry`, `KitchenObject`, `IKitchenObjectParent`, `ClearCounter`), `KitchenObjectSO` with Tomato + Cheese assets, walk animation hookup (`PlayerAnimator`). Test scene has two ClearCounters, a fridge prop, a pizza oven, and loose ingredients. Details in `/Docs`.

**Known debt to fix early:** `PlayerInteract` and `PlayerPickupDrop` are two parallel interaction paths that both raycast and both bind "Interact" (and `PlayerPickupDrop` bypasses `GameInput`, referencing the action asset directly). Consolidate before building counters on top (Phase 0).

---

# Roadmap

Work through phases in order; items within a phase are sized to be one sitting each. Check items off as they land, and split anything that turns out bigger than expected. Single-player-first: Phases 0–3 build the whole game loop offline; multiplayer conversion is its own phase (per Code Monkey's course, converting a finished SP game to NGO is very doable if we keep logic event-driven and centralized).

## Phase 0 — Foundation cleanup ✅ (completed Aug 2026)
- [x] **Upgrade to Unity 6.5** — on 6000.5.2f1; Cinemachine 3.1.7, NGO 2.13.1, Input System 1.20.0.
- [x] Merge `PlayerInteract` + `PlayerPickupDrop` into one interaction flow (one cast → contextual Pickup/Place/Use/Drop, all input via `GameInput`). Text prompts later removed by design — crosshair only.
- [x] Formalize Layers: 3 `PlayerBody`, 6 `Interactable`, 8 `Held` (see `Docs/SceneSetup.md`); interact/collision masks set from them. Watch-out: Main Camera has a hand-picked culling mask — new layers must be added to it.
- [x] Rename scene → `GameScene`; committed fridge prefab; gitignored `Assets/Screenshots/`.
- [x] Add `.gitattributes` (text normalization + binary marking; Force Text confirmed). LFS deliberately deferred — repo is small; revisit if clone size grows.
- [x] `KitchenObject.Spawn/DestroySelf` helpers — the only sanctioned create/destroy path.

## Phase 1 — Counters & stations (single-player)
- [x] `BaseCounter` abstract class implementing `IKitchenObjectParent` + `IInteractable`; `ClearCounter` is a subclass. Surfaces gained `CanAcceptKitchenObject()` so non-placeable stations (fridge) refuse placement.
- [x] Fridge: `FridgeCounter` with hinged right-door swing (Use toggles), interior shelf stock via `IngredientGrabPoint` display props (2 Cheese, 2 Meat) that spawn fresh copies into empty hands. Generic `ContainerCounter` kept for future crates. New `Meat` KitchenObjectSO + prefab (raw steak).
- [x] Trash: `TrashBin` — scriptless physical dumpster (open-top colliders); tossed items pile up and stay (Schedule 1-style) instead of despawning. Revisit despawn at Phase 3 if piles become a problem.
- [x] `PlatesCounter`: display-plate grab point dispenses plates (fridge-style grab, no timed spawning for now).
- [x] `Plate` kitchen object v1: `PlateHolder` gives it one food slot when on a counter (place food onto the plate; pick food off it; carry the plate with food riding along). Multi-ingredient contents model + valid-ingredient rules come with recipes (Phase 3).
- [ ] `CuttingCounter` (bar version first): place ingredient, interact to progress cut, `CuttingRecipeSO` (input → sliced output). Physics minigame replaces the bar in Phase 2.
- [ ] `StoveCounter`/oven (bar version first): state machine idle→cooking→cooked→burned via `CookingRecipeSO`; wire the PizzA oven prop; sizzle/warning hooks for later audio.
- [ ] `DeliveryCounter`: accepts a plate, validates against current orders.
- [ ] Selected-counter/object highlight visual driven by the unified interaction system.

## Phase 2 — Physics cooking minigames (the core twist)
Design doc: `Docs/Minigames.md` (written — grading framework + per-minigame designs). This phase is the game's identity; give it the most iteration time.
- [ ] **Prep grading system (`PrepScore`)**: per-action 0–100 score from measurable physics metrics (slice evenness, coverage %, cook-timing window, stack alignment), weights/tiers in `PrepQualitySO` data; scores stamp the prepped component and roll up into order payout in Phase 3. See `Docs/Minigames.md`.
- [ ] **Held-tool system**: player can hold tools (knife, spatula, sauce ladle) with first-person animations; extend carry system to distinguish tools from ingredients.
- [ ] **Chopping v1**: knife follows a constrained swing; ingredient starts as pre-cut chunks joined by breakable fixed joints (real mesh slicing later only if worth it). Cut quality = slice count/evenness.
- [ ] **Sauce/marinade brushing v1**: surface with a coverage mask (steak marinade, garlic butter on bread); brush paints via raycast splat-map, success = % coverage.
- [ ] **Plating/assembly v1**: physically arrange steak + sides on the plate; snap-with-tolerance so it's tactile but not rage-inducing; sloppiness affects order score.
- [ ] **Pan/stove v1**: pan is a physics container, flip gesture for patties; food visibly changes state (raw→cooked→burned materials + smoke VFX hook).
- [ ] Replace the CuttingCounter/StoveCounter bar interactions from Phase 1 with these minigames behind the same recipe SO data.
- [ ] Playtest pass: tune each minigame to be learnable in <1 minute (they must be teachable in the break room later).

## Phase 3 — Game loop (single-player complete)
- [ ] `RecipeSO` (final dish = list of KitchenObjectSOs) + `RecipeListSO`.
- [ ] `OrderManager`: spawns orders on a cadence; active-order cap and spawn rate scale with player count (design the scaling formula, document it).
- [ ] Orders UI: ticket list with ingredients, per-order countdown, success/fail feedback.
- [ ] `GameManager` state machine: WaitingToStart → Countdown → Playing → GameOver, match timer.
- [ ] Scoring: per-order money reward, quality multiplier from minigame performance; run-summary screen.
- [ ] Food integrity: items dropped on the floor (or knocked from hands) become **dirty/wasted** — visual change + worthless for orders; trash or keep as clutter.
- [ ] Pause menu + options (sensitivity, volume) with `GameInput` map switching (Player ↔ UI).
- [ ] First-person polish pass: interact prompts as world-space UI on stations, crosshair states.

## Phase 4 — Multiplayer (NGO + UGS)
Follow the Kitchen Chaos multiplayer course structure, adapted to first-person.
- [ ] Decide + document authority model: server-authoritative kitchen objects and counters, owner-authoritative player movement. `Docs/Multiplayer.md`.
- [ ] Convert Player to `NetworkBehaviour`: ownership checks, spawn points, client network transform for movement, sync look direction for head/animation.
- [ ] Player-player physicality: chefs get colliders and can bump each other; a hard bump knocks the held item out of the victim's hands (→ dirty/wasted food rule from Phase 3).
- [ ] Convert KitchenObject to `NetworkObject`: spawn/despawn through server, NGO parenting instead of raw `transform.SetParent`; decide how the *physics drop* state replicates (server-sim transform sync is fine).
- [ ] Convert all counters + OrderManager + GameManager to networked (server runs logic; clients get NetworkVariables/ClientRpcs for feedback).
- [ ] Minigame networking: minigame runs on the interacting client, client reports result, server validates plausibility and applies outcome (keeps physics feel local; document per-minigame in `Docs/Minigames.md`).
- [ ] UGS setup: project link, anonymous Authentication.
- [ ] Lobby service: create/join (code + quick join), lobby list UI, heartbeat/poll.
- [ ] Relay: allocate on host, join via code, wire into `UnityTransport`; keep all UGS bootstrap in one `MultiplayerBootstrap` class so a Steam transport can slot in later.
- [ ] Disconnect handling: host migration is out of scope — clean "host left" flow back to menu; a disconnecting player drops their held object.
- [ ] 2-instance test workflow: Multiplayer Play Mode package + a written smoke-test checklist.

## Phase 5 — Voice & audio
- [ ] Vivox setup: positional (3D) channel joined on entering lobby/game; audio position taps the player head transform.
- [ ] Tune falloff so voice range ≈ half the kitchen; verify far players are genuinely hard to hear, and direction is audible with headphones.
- [ ] Push-to-talk vs open-mic option; speaking indicator over character heads.
- [ ] Diegetic SFX pass: stove sizzle, timer beeps, chopping thunks, order-ticket printer — all 3D spatial sources so they compete with voice on purpose.
- [ ] Mixer setup: buses for Voice / SFX / Music.
- [ ] Music: main theme + in-game loop that intensifies in the last 60s (compose with AI tools or license; track choices in `Docs/Audio.md`).

## Phase 6 — Procedural kitchen layout
- [ ] Design doc `Docs/KitchenGeneration.md`: grid-based kitchen; rules (delivery reachable, required stations present, min spacing, space scales with player count).
- [ ] Author station footprint data (each counter prefab = grid size + orientation constraints).
- [ ] Generator v1: seeded random placement into wall/island slots on a fixed floor plan; validate with reachability flood-fill; retry on invalid.
- [ ] Generator v2: vary floor plan shape (L, U, split rooms with pass-through windows).
- [ ] Network sync: server picks seed, clients generate deterministically from it.
- [ ] Tooling: editor button to regenerate + preview layouts fast; keep 2–3 handmade layouts as fallback/tutorial kitchens.

## Phase 7 — Physical lobby (break room)
- [ ] Break-room scene: tables, lockers, door to kitchen; players walk around in first person.
- [ ] Ready-up interaction (interact with locker/clipboard to toggle ready); host gets Start Game when all ready; countdown + transition via NGO scene management.
- [ ] Practice stations: fully functional cutting/stove/assembly stations with infinite ingredients and no timer — reuses the real minigames as the game's tutorial.
- [ ] Recipe book prop: browse all recipes + minigame instructions.
- [ ] Customization mirror/locker: preview and equip owned cosmetics (hooks into Phase 8).

## Phase 8 — Progression & cosmetics
- [ ] Money persistence: match earnings → saved wallet. Local JSON save first; UGS Cloud Save later if wanted.
- [ ] `CosmeticSO` (aprons, hats, +future slots) + shop UI in the break room.
- [ ] Equip system: cosmetic attach points on the character rig, synced over network so others see your hat.
- [ ] Ownership save data; server-validated purchases in MP.

## Phase 9 — Game modes
- [ ] Mode framework: `GameModeSO` (timer length, order scaling curve, win condition) consumed by GameManager.
- [ ] **Party mode** (default; ≈ the Phase 3 loop): X minutes, high score.
- [ ] **Endless mode**: orders accelerate until N concurrent orders expire; "survived time + orders" score.
- [ ] **VS mode** (biggest lift — last): two teams, mirrored generated kitchens, same order feed, most completed orders wins; needs 8-player sessions, team assignment in lobby, split scoreboards. Cut scope here first if needed.

## Phase 10 — Art, animation & content pipeline
- [ ] Audit Pandazole + PizzA packs: which stations/ingredients are covered, gap list for custom assets → `Docs/ArtDirection.md`.
- [ ] Lock a look (low-poly stylized matches the packs): palette + one URP post volume profile.
- [ ] Chef character: simple rigged low-poly chef (pack or Blender via MCP); needs head-look + carry + minigame gesture animations. Decide full-body-with-camera-clipping vs separate FP arms, and doc it.
- [ ] **FP carry hands (Peak-style)**: visible first-person hands holding carried items out front with both hands while moving (two-hand grip pose + subtle sway); IK the hands onto the physics-driven held item so they track its wobble.
- [ ] Ingredient state variants (whole/sliced/cooked/burned) for every recipe ingredient — Blender MCP where packs fall short.
- [ ] VFX: smoke, steam, sauce splat, order-complete confetti (cheap particle systems).
- [ ] Full recipe content pass (steakhouse menu): steak dinners (rare→well-done), sides (salad, mashed potatoes, mac & cheese — the Pandazole pack has these), + 2–3 more dishes with all SOs, prefabs, minigame tuning.

## Phase 11 — Polish & release
- [ ] Menus: title, settings (audio/video/controls/sensitivity), mode select, lobby browser.
- [ ] Onboarding: first-run hint prompts in the break room.
- [ ] Performance pass: profile 4 players + full kitchen; pool spawned ingredients; URP batching check.
- [ ] Builds: Windows + macOS configs; test cross-platform MP (Mac host ↔ PC client via Relay).
- [ ] Playtests with real humans; a cut/keep list from feedback.
- [ ] If shipping on Steam: Steamworks decision point (transport swap, Steam lobbies, achievements).

---

## Working agreements for Claude

- Use MCP for Unity for scene/prefab/component/SO work (verify visually with screenshots, check the console after script changes); fall back to precise editor wiring instructions only if the connection is down.
- After any system work, update its `/Docs/<System>.md` (create it if new) — the doc, not chat history, is the source of truth across the user's Mac/PC switch.
- Keep gameplay logic event-driven (C# events on managers/counters, UI subscribes) — this is what makes the NGO conversion in Phase 4 tractable.
- Never build a feature networked-first before Phase 4; never build a feature that *can't* be networked (no client-only authoritative state, no gameplay logic living in UI).
