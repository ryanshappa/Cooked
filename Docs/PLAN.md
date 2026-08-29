# Yes Chef — Technical Plan & Architecture Decisions

This document records the big technical decisions for the project — what we chose, what we considered, and why — plus the implementation plan for each. The task-level roadmap lives in [`../CLAUDE.md`](../CLAUDE.md); this is the "why" behind it. Last reviewed: **August 2026**.

> **Revised 2026-08-28 — Decisions 2 and 3 were replaced.** The netcode is now **FishNet + Facepunch Steamworks** and the voice is **Dissonance**, both **ported from the sibling project `C:\Unity\Projects\Extraterrestrial`** (same dev, same Unity version, working code). The superseded NGO/UGS and Vivox reasoning is kept at the bottom under "Superseded decisions" — it explains why the *old* answer looked right, which matters if the new stack ever has to be re-argued.

Guiding principle: we build on the foundation already in place (Code Monkey-style architecture — ScriptableObject-driven data, small interfaces like `IInteractable`/`IKitchenObjectParent`, prefab-per-thing, `_Assets` folder organization, event-driven managers). Every decision below was weighed partly on "does it preserve that way of working."

---

## Decision 1 — Unity version: upgrade to Unity 6.5

**Choice: Yes — upgrade from 6000.1.15f1 to Unity 6.5 (6000.5.x), now, before any more systems are built.**

Reasoning:
- Unity 6.5 shipped **June 2026** as a *Supported* release — same stability/critical-fix treatment as LTS. Our 6000.1 (April 2025) is several releases behind and no longer the supported line.
- Within the Unity 6 generation, updates are explicitly designed to be **non-breaking** (no API compatibility breaks between 6.x releases), so this is the cheapest an upgrade will ever be. It only gets riskier as we add scenes, prefabs, and networked code.
- What we actually gain: current URP (better on-tile post-processing, Light Explorer), continued CoreCLR progress, and — most relevant to us — the maturing multiplayer tooling generation (Multiplayer Center, **Multiplayer Play Mode** for in-editor multi-client testing, which we rely on heavily in Phase 4).
- Nuance worth knowing: the multiplayer stack itself (Netcode for GameObjects, Multiplayer Services SDK, Vivox) lives in **packages** that update independently of the editor. The editor upgrade matters for staying on the supported line and tooling; the netcode wins come from the packages either way.

**Implementation plan:**
1. Commit everything first (clean working tree).
2. Upgrade on ONE machine via Unity Hub → open project → let it re-import; fix any warnings; run the game; commit `ProjectSettings/ProjectVersion.txt` + any migrated assets.
3. Install the exact same 6000.5.x on the other machine before pulling. **Rule from here on: both machines always run the identical editor version; the version bump is always its own commit.**
4. While we're at it, update NGO and Input System packages to their latest compatible versions.

---

## Decision 2 — Multiplayer: FishNet + Facepunch Steamworks (ported from Extraterrestrial)

**Choice: FishNet 4.7.2 (client-hosted) over Facepunch.Steamworks, with the connection layer ported from the Extraterrestrial project rather than written fresh. Steam is the shipping transport from day one (dev appid 480), with FishNet's Tugboat UDP transport alongside it via Multipass so the editor and LAN work without Steam running. NGO + UGS Sessions + Vivox are dropped.**

### Why this changed (2026-08-28)
The sibling project `C:\Unity\Projects\Extraterrestrial` — same developer, **same Unity 6000.5.2f1**, same URP, also PC/Steam co-op — already built and shipped-to-playtest this exact stack: Steam lobby → overlay invite → multiple clients in one scene, with proximity voice riding the same connection. That is a working, debugged, *already-paid-for* implementation of the two hardest, least-fun parts of this project (lobby/transport plumbing and voice). Rebuilding it on a different SDK would be doing the same work twice, badly, for no gain.

The decisive facts:
1. **It exists and it runs**, on the identical editor version, with the local patches already found and documented (see Risks). Porting is measured in files, not in weeks.
2. **Steam is where this ships anyway.** The old plan treated Steam as a Phase-11 transport swap; in practice, Steam lobbies + overlay invites *are* the multiplayer UX for a 1–4 player co-op game bought on Steam. Building the join-code/Relay path first and replacing it later was always going to be throwaway work.
3. **One connection, no cloud services, no bills.** FishNet + Steam P2P means no UGS project, no Relay/Lobby free-tier ceilings, no per-CCU anything, and voice rides the same transport instead of a second service.
4. **The switch is free right now, and only right now.** Phase 4 has not started: `grep` finds zero `Unity.Netcode` references in `Assets/_Assets/Scripts`. Every month we wait, this decision gets more expensive.
5. **Unity physics stays the single simulation**, exactly as before — the reason Photon Quantum was rejected is unchanged and still applies.

### The stack
| Piece | Source | Role |
|---|---|---|
| **FishNet 4.7.2** (free) | FirstGearGames — imported into `Assets/` | netcode core |
| **Facepunch.Steamworks 2.5.2** | facepunch/Facepunch.Steamworks release DLLs | Steam API: lobbies, identity, overlay invites |
| **FishyFacepunch 4.1.0** | FirstGearGames/FishyFacepunch | FishNet transport over Steam P2P (SteamNetworkingSockets) |
| **Tugboat** | ships with FishNet | plain UDP for editor/localhost/LAN — no Steam in the loop |
| **Multipass** | ships with FishNet | runs both transports at once; the client picks at connect time |
| Steam **appid 480** (Spacewar) | Valve's public test appid | dev-only until Yes Chef has a Steam page |

### Architecture (the shape being ported — `Assets/_Assets/Scripts/Net/`)
Three small files, mirroring Extraterrestrial's proven split. The old plan's "all connectivity behind one `MultiplayerBootstrap`" rule survives — it just becomes three named seams instead of one grab-bag:
- **`SteamBootstrap`** — `SteamClient.Init(480)` behind the editor-without-Steam guard: Steam missing → Steam features disable with one log line and everything else keeps working. Nothing but `LobbyService` may call the Steam API directly.
- **`NetworkBootstrap`** — owns the FishNet `NetworkManager` + Multipass. Public verbs only: `HostLocal()`, `JoinLocal(address)`, `HostSteam()`, `JoinSteam(steamId)`, `Leave()`. The rest of the game never touches transport types.
- **`LobbyService`** — Facepunch lobbies: create (max 4) on host, overlay invite, `OnGameLobbyJoinRequested` → read host SteamId from lobby data → `JoinSteam(hostId)`. Discovery and invites only; game traffic runs on the transport, so this file stays small.
- **`NetDevPanel`** — dev-only host/join/invite buttons + connection readout. Ported as-is; it is how two-client tests stay a five-minute ritual.

### Authority model (unchanged from the old plan, and it matches the port)
**Server(host)-authoritative kitchen: counters, kitchen objects, orders, and the match clock. Owner-authoritative player movement.** Extraterrestrial's invariant — "characters are owner-authoritative; the host is authoritative for all world state" — is the same split, and its `ItemOwnership` single-pathway rule for grab = ownership transfer is directly applicable to `KitchenObject` pickup. Simulator actions (chopping, slicing, pouring) still run locally on the acting client, which reports a result the host sanity-checks — the cut plane replicates as `(origin, normal)`, not as vertex data (see `MeshSlicing.md`).

### Risks and the mitigations already known from the sibling project
- **FishNet 4.7.2 does not compile clean on Unity 6000.5** — `Scene.handle` int conversions and `Object.GetInstanceID()` are obsolete-as-error. Extraterrestrial fixed this with 15 one-token call-site edits routed through a single shim file (`ExpeditionUnity65Compat.cs`). **This is why we copy their patched `Assets/FishNet/` folder rather than re-importing FishNet** — see the Port inventory. There are three patches in total, all listed there. Delete them the day upstream migrates.
- **Version pairing:** FishyFacepunch historically lags FishNet releases. Rule (inherited): **FishyFacepunch's supported FishNet version wins** — pin FishNet to match, and log any forced downgrade here.
- **Steam-missing behavior is a trap:** FishyFacepunch does *not* throw when Steam is absent — it logs an error and returns false, so a `try/catch` never fires. Extraterrestrial hit this and it killed local hosting whenever Steam wasn't running. The fix is ordering: `HostLocal()` starts Tugboat **by index** and Steam best-effort; `HostSteam()` does the reverse.
- **Steam overlay invites don't inject reliably** — keep the join-by-SteamId fallback that `NetDevPanel` already has.
- **Not first-party.** FishNet is a community package; if it were abandoned we would be on our own. Accepted: the source is in `Assets/`, we already patch it, and a 4-player co-op game is not exotic netcode.
- **The Kitchen Chaos multiplayer course no longer maps 1:1.** Its *architecture* lessons (server-auth kitchen objects, event-driven managers, spawn through one seam) still apply and are what the single-player phases were built for; only its NGO API calls are now reference-only.

**Implementation plan (Phase 4, revised):**
1. Remove NGO + Multiplayer Center from `Packages/manifest.json` and delete `Assets/DefaultNetworkPrefabs.asset` (no code depends on them).
2. Import FishNet + Facepunch DLLs + FishyFacepunch; re-apply the Unity 6.5 compat shim; project compiles, single-player press-play unchanged.
3. Port `SteamBootstrap` / `NetworkBootstrap` / `LobbyService` / `NetDevPanel` from Extraterrestrial, renaming the project-specific bits.
4. Localhost duo first (Tugboat, two chefs walking), *then* the Steam path (lobby + invite, two accounts). Steam variables never enter the picture until the plain loop works.
5. Convert gameplay in the roadmap's order: Player → KitchenObject → counters → managers.

---

## Decision 3 — Voice: Dissonance over FishNet (ported from Extraterrestrial)

**Choice: Dissonance Voice Chat (Asset Store, already owned) with the community `DissonanceVoiceForFishNet` shim vendored as owned source. Vivox is dropped.**

Voice packets become FishNet broadcasts on the session's existing transport — Tugboat locally, FishyFacepunch over Steam — host-relayed. **No second connection, no cloud service, no account, and it works in editor-without-Steam exactly as far as FishNet does.** Proximity is pure client-side 3D audio on the playback `AudioSource`, which is all our "can barely hear them across the kitchen" mechanic ever needed.

Why it wins here specifically:
- **It rides the connection we already have.** Vivox would mean a second service, a second identity, and a UGS project we otherwise no longer need (Decision 2 removed our only other reason to have one).
- **Already bought and already debugged.** Extraterrestrial purchased Dissonance 9.0.9 and vendored the FishNet shim under `Scripts/Net/DissonanceFishNet/` — needing three upstream bug fixes on day one (a no-op `OnDisable` coroutine; a `PlayerId` lazy-cache that defeated its own change guard so **positional tracking never started**; an unsafe-code requirement). Those fixes come with the port. Discovering them a second time from scratch would cost days.
- **Tuned values transfer.** The mic modes, codec settings, and noise-suppression findings in Extraterrestrial's `Docs/TUNING.md` ("Proximity voice") are the result of real two-account sessions — e.g. Dissonance stacks *two* noise suppressors and the RNN "remove everything that isn't speech" pass is what makes voices sound watery; **RNN off + WebRTC denoise Moderate** was the fix. Codec: Opus 48 kHz, quality High (24 kbps), FEC on. Doppler **0** on the voice source (a moving speaker warbles otherwise).

### What must be re-tuned for a kitchen (do not copy blindly)
Extraterrestrial is an open-world planet game; its rolloff is **logarithmic, min 10 m / max 1000 m**. Our kitchen is ~13×8 m — those values would make every chef perfectly audible everywhere and delete the pillar. Yes Chef needs a much tighter curve (starting point: **min ≈ 2–3 m, max ≈ 15–20 m**, tuned so a chef at the far wall is genuinely hard to make out), and the canyon-echo/obscurance layers are irrelevant here. **The falloff test from the roadmap stands: voice range ≈ half the kitchen.**

### Structural rule worth inheriting
Voice lives on a **dedicated net rig prefab** (`DissonanceComms` + the FishNet comms component + room triggers), **not** woven into the character prefab — Extraterrestrial's `takeaways.md` flags prefab-woven voice as Photon-shaped noise. The player prefab carries only a small tracker component + a `VoiceAnchor` transform at eye height. Every networked scene gets voice by construction from that one prefab. For us the anchor is the FP camera/head position.

**Implementation plan (Phase 5, revised):**
1. Import Dissonance + vendor the FishNet shim (with its README of local fixes) once Phase 4's connection works.
2. Net rig prefab with `DissonanceComms`, one global room, positional playback; `VoiceAnchor` on the chef at eye height.
3. Tune rolloff to the kitchen (above) and verify by ear: far chef ≈ unintelligible, direction audible on headphones.
4. Mic modes: Voice Activation default (VAD), PTT/PTM selectable; speaking indicator over heads; per-player volume/mute via Dissonance's `VoicePlayerState.Volume` / `IsLocallyMuted`.
5. Diegetic SFX pass + mixer buses (Voice / SFX / Music) — the audio-chaos-as-gameplay design is unchanged.

---

## Port inventory — what actually comes over from Extraterrestrial

Verified by a full read of their project on 2026-08-28 (source root `C:\Unity\Projects\Extraterrestrial`). **Their `Packages/manifest.json` contains nothing networking-related — the entire stack is vendored under `Assets/` and committed to git.** That is the single best fact about this port: it is copying folders, not resolving packages.

### Copy these folders wholesale
| Source folder | What it is | Why wholesale |
|---|---|---|
| `Assets/FishNet/` | FishNet 4.7.2 + Tugboat + Multipass + **FishyFacepunch 4.1.0**, all **already patched** | **Do not re-import FishNet fresh.** A clean 4.7.2 does not compile on Unity 6000.5 and their three patches (below) live inside this tree. Copying it brings the fixes; re-importing means rediscovering them. |
| `Assets/AssetStore/Facepunch/` | Facepunch.Steamworks 2.5.2 — managed DLLs + `redistributable_bin/` natives | Plugin import settings live in the `.meta` files — copy those too. |
| `Assets/Plugins/Dissonance/` **and** `Assets/Dissonance/` | Dissonance 9.0.9 (runtime + integrations), unmodified. Note the pack uses **two folders**; leave them where the importer puts them. | Includes `Resources/VoiceSettings.asset` — the playtested codec/suppression config (below). **Paid Asset Store pack: copying it between your own projects is a licensing question for you to confirm, not a technical one.** |
| `Assets/Scripts/Net/DissonanceFishNet/` | The MIT FishNet↔Dissonance shim (12 files) **with its three local bugfixes**, documented in its own `README.md` | Never re-download upstream — see Decision 3. Upstream's two `.asmdef` files were deliberately dropped (their code compiles into `Assembly-CSharp`, and so does ours — compatible). |

### Copy these scripts (`Assets/Scripts/Net/` → our `Assets/_Assets/Scripts/Net/`)
| File | Lines | Port? | Notes |
|---|---|---|---|
| `NetworkBootstrap.cs` | 185 | ✅ **drop-in** | `HostLocal`/`JoinLocal`/`HostSteam`/`JoinSteam`/`Leave`. Carries both hardening fixes: per-index transport start, and a `PortFree()` raw-bind probe that walks up to 5 ports past a **leaked Tugboat socket** (editor play-exit doesn't reliably close it; the port squats for minutes). |
| `SteamBootstrap.cs` | 49 | ✅ **drop-in** | `[DefaultExecutionOrder(-1000)]` so it beats FishyFacepunch's own init; early-returns on `SteamClient.IsValid` (re-init footgun). Change the appid when we have one. |
| `LobbyService.cs` | 127 | ✅ **drop-in** | Create (`SetFriendsOnly`, host SteamId in lobby data) / enter / `OnGameLobbyJoinRequested` (covers both invite-accept **and** overlay "Join Game") / `InviteFriend(SteamId)` direct invites. `maxMembers 4` already matches us. |
| `NetDevPanel.cs` | 139 | ✅ **drop-in** | Host/join/invite buttons, typed-SteamId join fallback, plus `-autohost`/`-autoclient`/`-autohoststeam` CLI flags for scripted two-client tests. |
| `PlayerIdentity.cs` | 80 | ✅ adapt | SteamName → ServerRpc → `SyncVar<string>`, with **input sanitising** (strips `<`,`>`, control chars, clamps 24) before it reaches any label. Keep the sanitiser. |
| `NetRigSpawns.cs` | 33 | ✅ adapt | Fills `PlayerSpawner.Spawns` from scene objects named `SpawnPoint*`. Ours become kitchen entrance spawns. |
| `Voice/VoiceSettings.cs` | 169 | ✅ adapt | Mic device/mode/volume + PlayerPrefs. **Implements PTT/PTM as `CommActivationMode.Open` + `IsMuted`**, driven by a new Input System action — Dissonance's own PushToTalk polls the legacy Input Manager we don't use. |
| `Voice/VoiceDebugReadout.cs` | 45 | ✅ as-is | Who's speaking / amplitude / tracked / distance. |
| `UI/VoiceIndicators.cs`, `UI/PlayerNameplates.cs` | ~200 | ✅ adapt | Speaking dots + nameplates on the "looked at OR speaking" gate. Ours re-tunes distances to kitchen scale. |
| `Voice/VoiceObscuranceFilter.cs` | 121 | ⚠️ **copy but bypass** | Wall-muffling + 60 m canyon echo, tuned for open terrain. It sits **on the playback prefab**, so it arrives with it — set `bypass` on and revisit only if voice-through-walls bothers a playtest. |
| `NetCharacter.cs`, `CharacterState.cs`, `RemoteCharacterDriver.cs`, `NetGhostHarness.cs`, `NetReplicaHarness.cs` | ~800 | ❌ **do not port** | Built for a hover-root rigidbody with drive forces and a control scalar; our `Player.cs` is a manual capsule-cast controller with no rigidbody. **Their RPC shape is still the template**: owner samples state on `TimeManager.OnTick` at 15 Hz → unreliable `ServerRpc` → `ObserversRpc(ExcludeOwner, ExcludeServer)`, discrete events on reliable RPCs, and a `TargetRpc` so a remote initiator *asks* the owner to apply an effect rather than writing it. |
| `NetPlayerCapsule.cs` | 41 | ➖ scaffolding | Their throwaway test capsule. Useful once, as the first "two things move" smoke test. |

### Prefabs — and the one that needs surgery
- **`Assets/Prefabs/Net/VoicePlayback.prefab`** — copy as-is, then re-tune the `AudioSource` (see Decision 3: their Logarithmic min 10 m / max 1000 m is planet-scale). Doppler 0 and full-3D spatial blend stay.
- **`Assets/Prefabs/Net/NetRig.prefab`** — their whole multiplayer rig in one prefab: a `NetworkManager` child (NetworkManager + Tugboat + FishyFacepunch + Multipass + TransportManager + PlayerSpawner + all four bootstrap scripts) and a `Voice` child (`DissonanceComms` → playback prefab, the FishNet comms component, a **`Global`-room** `VoiceBroadcastTrigger` with `broadcastPosition` ON + VoiceActivation, a matching `VoiceReceiptTrigger`, and the voice UI). **Take those two children and leave the rest.** Their NetRig also carries `Main Camera` (with the **`AudioListener`**), a Cinemachine FP rig, and their eye rig — we already have all of that in `GameScene`. Dissonance requires **exactly one AudioListener**; blindly instancing their prefab gives us two cameras and two listeners.
- **Multipass transport order matters**: index 0 = Tugboat, index 1 = FishyFacepunch. `NetworkBootstrap.IndexOf<T>()` resolves by type, but their prefab's list order is the tested one.
- `DefaultPrefabObjects.asset` is FishNet's spawnable-prefab registry — **do not copy theirs**, FishNet regenerates ours.

### The three local patches we inherit (all inside `Assets/FishNet/`)
1. **`Runtime/ExpeditionUnity65Compat.cs`** — the Unity 6000.5 blocker. The editor made `Scene.handle` int conversions and `Object.GetInstanceID()` **obsolete-as-error**; FishNet 4.7.2 (and upstream `main`) are unmigrated. Two extension methods (`HandleInt()`, `InstanceIdInt()`) plus **15 call-site edits** across 6 files: `Managing/Scened/SceneLookupData.cs` ×2, `Managing/Scened/SceneManager.cs` ×8, `Managing/Scened/UnloadedScene.cs` ×2, `Observing/NetworkObserver.cs` ×1, `Serializing/Helping/Comparers.cs` ×1, `Serializing/SceneComparer.cs` ×1.
2. **`Plugins/FishyFacepunch/FishyFacepunch.cs`** (~line 100) — upstream `SteamClient.Init` throws with no Steam running, taking Tugboat down with it. Wrapped. Grep marker: `EXPEDITION local patch`.
3. **`Plugins/FishyFacepunch/Core/ServerSocket.cs`** (~line 135) — `_socket.Close()` throws an internal NRE when Steam tears down before the transport on play-stop. Caught.

Rename the shim (it says "Expedition") but **keep the greppability** — one file, named call sites. Delete it the day upstream migrates.

### Two setup steps that are easy to miss
- **`InputSystem_Actions`** needs a `Player/PushToTalk` action (their binding: **V**). `VoiceSettings` looks it up by that exact string via `InputSystem.actions.FindAction("Player/PushToTalk")` — a missing action fails silently.
- **Exactly one `AudioListener`** in the scene (see NetRig above), and `SpawnPoint*`-named empties for `NetRigSpawns`.

### Testing gotchas inherited with the stack
- **Appid 480 (Spacewar):** both players' games must already be running *before* an invite is accepted, or Steam launches the actual Spacewar. Lobbies are friends-only, so testers must be Steam friends.
- **The Steam overlay does not inject into the Unity editor** — that is why the direct `InviteFriend` and typed-SteamId paths exist. Editor-hosts-build is the practical two-client setup.
- **Two instances on one machine share one microphone** → headphones required, or the second client's playback loops back into the mic.
- **Dissonance player ids must be unique per session**, so they are GUIDs, not SteamIds (one machine, two clients, one Steam account). Display names ride separately on `PlayerIdentity`.
- **`SceneManager.LoadScene` is banned in networked flow** — FishNet scene management only. Relevant to us at Phase 7 (break room → kitchen).

**The honest scope line:** what we get for free is *connection, lobby, invites, and voice* — the plumbing, and it is genuinely drop-in. What we still write ourselves is *every gameplay conversion* (Player, KitchenObject, counters, OrderManager) — the bulk of Phase 4, which no port shortens.

---

## Decision 4 — Procedural kitchen layouts: build our own small seeded generator (no asset)

**Choice: custom grid-based generator. The well-known assets solve a different problem than ours.**

What's out there: **Edgar Pro** (~$55, graph-of-rooms + handmade room templates — the Enter the Gungeon approach), **Dungeon Architect**, and various roguelite kits. These are all **room-and-corridor topology generators**: they decide how *rooms* connect. Our randomization need is one level finer: **station/furniture placement within essentially one room**, under gameplay constraints (every required station present, delivery window reachable, minimum walkway width, fridge-to-counter distances sane, floor size scaled to player count). No asset solves that layer — we'd fight a dungeon tool to do furniture placement.

And the honest scope assessment: our generator is *small*. A kitchen is a grid of wall-slots and island-slots; stations have footprints and orientation rules; placement is seeded-random with constraint checks and a flood-fill reachability validation, retry on failure. That's a few hundred lines we fully control and can guarantee **deterministic** (host picks a seed, shares it via session properties, every client generates the identical kitchen — no need to network the layout itself).

Edgar Pro stays on the shelf as a *maybe-later*: if Phase 6 v2 grows into multi-room restaurants (dining room, freezer room, pass-through windows), a graph-of-rooms generator for the macro shape + our generator per room would compose nicely. Not before then.

Implementation plan (Phase 6, as in the roadmap):
1. Design doc first (`KitchenGeneration.md`): grid spec, slot types, per-station footprint + placement rules, validation rules, player-count scaling.
2. Station prefabs get a small `StationFootprint` data component (grid size, allowed slot types, clearance).
3. Generator v1: fixed rectangular floor plan, seeded placement into slots, flood-fill validation, retry loop. Editor "Regenerate" button for rapid iteration.
4. Generator v2: floor-plan shape variation (L / U / island configurations).
5. Determinism guardrails: single `System.Random(seed)` instance, no order-dependent iteration over Unity collections, and an automated test that generates N seeds twice and diffs the results.
6. Keep 2–3 handmade layouts (tutorial kitchen, fallback, break-room practice area).

---

## Decision 5 — Assets: placeholder-first, swap-friendly structure

**Choice: keep building on the placeholder packs (Pandazole Ultimate Pack, PizzA), buy or create final art later, and structure prefabs so the swap is cheap.**

The rules that make "replace art later" painless:
1. **Logic/visual split in every gameplay prefab.** The root prefab owns colliders + gameplay components + anchor points (`counterTopPoint`, hold points); the *visual* is a child (ideally its own nested prefab). Swapping art = replacing the visual child; no scene, wiring, or code changes. (`ClearCounter` already works this way — keep it that way for every station and ingredient.)
2. **ScriptableObjects are the only place content is defined.** New ingredient/recipe/cosmetic = new SO asset + prefab, zero code. This is the Code Monkey pattern we're keeping.
3. Placeholder art is *shippable-quality blocking*, not junk — consistent scale (real-world meters), pivots at the base, one material style — so layouts and reach distances tuned now stay valid after the art swap.
4. When we make custom assets: **Blender via the connected MCP** for simple ingredient states (whole/sliced/cooked variants), purchased packs for big-ticket items (chef character, kitchen set) if the gap list justifies it. The audit of what the current packs cover vs. what recipes need happens in Phase 10 (`ArtDirection.md`).
5. Animations and music are the highest-risk "never done this" areas — plan is: start with simple Animator blend trees (already working for walk), use asset-pack/Mixamo-style animations where possible, and defer bespoke animation until the game is fun with placeholders. Music: license or AI-compose late (Phase 5/10), tracked in `Audio.md`.

---

## How this maps to the roadmap

Nothing in these decisions changes the phase order in `CLAUDE.md` — they de-risk it:
- **Now:** Unity 6.5 upgrade (Decision 1) slots in as the first Phase 0 task, before the interaction-system merge.
- **Phases 1–3** are unaffected — pure single-player Unity work on the existing foundation.
- **Phase 4** ports the FishNet + Steam connection layer from Extraterrestrial (Decision 2) instead of following the course's NGO/UGS path; the course's *architecture* lessons still apply, its API calls no longer do.
- **Phase 5** is Dissonance on that same connection (Decision 3), with kitchen-scale rolloff re-tuned from scratch.
- **Phase 6** is the custom generator (Decision 4).
- **Phase 10–11** carry the asset swap (Decision 5). The Steam "decision point" is gone — Steam is in from Phase 4; what remains at Phase 11 is the real appid, the store page, and achievements.

## Superseded decisions

Kept for the reasoning, not the conclusion. Both were replaced on 2026-08-28 by the port from Extraterrestrial (Decisions 2 and 3 above).

<details>
<summary><b>Superseded Decision 2 — NGO + Unity Multiplayer Services (Sessions), Steamworks later as a transport swap</b></summary>

### Original: Decision 2 — Multiplayer: Netcode for GameObjects + Unity Multiplayer Services SDK (Sessions), Steamworks later as a transport swap

**Choice: NGO 2.x (client-hosted, server-authoritative) as the netcode — with the transport treated as deliberately swappable. Develop against Unity Transport + Relay/Sessions (no Steam appid needed, works in-editor with Multiplayer Play Mode/ParrelSync); at launch, drop in the community Facepunch Steamworks transport from Unity's [multiplayer-community-contributions](https://github.com/Unity-Technologies/multiplayer-community-contributions) repo for Steam invites and friend lists. Don't pick one transport and commit — NGO sitting on a swappable transport layer is the point of choosing it. Photon Quantum: no.**

The options considered:

| Option | What it is | Verdict for us |
|---|---|---|
| **NGO + Multiplayer Services SDK** | Unity's first-party high-level netcode over GameObjects; Sessions API is the 2025+ replacement for hand-wiring Lobby+Relay+Auth — one `CreateSessionAsync` call gives you a lobby with join codes and Relay transport wired into NGO | ✅ **Chosen** |
| **Photon Quantum** | Deterministic prediction/rollback ECS engine. Excellent tech, Unity Verified — but it runs its **own fixed-point deterministic physics simulation**, not Unity PhysX | ❌ Disqualifying mismatch: our entire minigame pillar is built on *Unity* physics (Rigidbodies, joints, capsule casts). Quantum would mean rewriting every gameplay system in its ECS + its physics, abandoning the Code Monkey architecture and most existing code. Rollback determinism is for competitive latency-sensitive games (fighting, sports); a 1–4 player co-op kitchen doesn't need it. Plus per-CCU pricing. |
| **Photon Fusion / PUN** | Photon's state-transfer netcode; closest real competitor to NGO | ❌ Good tech, but paid CCU pricing, and it buys us nothing NGO doesn't already do at our scale (≤8 players) while giving up first-party integration and the Kitchen Chaos course as a reference path. |
| **Facepunch.Steamworks alone** | A C# wrapper for the Steamworks API (Steam lobbies, friends, invites, achievements, Steam networking) | ⚠️ Not a netcode SDK — it complements one. The community "Steam transport for NGO" lets NGO run over Steam's relay instead of Unity Relay, with friend invites instead of join codes. That's exactly how we'll use it **if/when we ship on Steam**. |

Why NGO + Sessions wins on the merits (not just inertia):
- **Already installed and already the plan of record** — the Code Monkey multiplayer course we're following is NGO + UGS, so we have a proven, documented conversion path for exactly this genre.
- The **Sessions API** eliminates the historical pain of that stack: what used to be ~4 services of boilerplate (Auth, Lobby heartbeats, Relay allocation, transport wiring) is now one session object with join codes, quick-join, and player properties.
- **Same ecosystem as Vivox** (Decision 3) — one UGS project, one Authentication sign-in shared by sessions and voice.
- **Free at our scale**: Relay/Lobby free tiers comfortably cover development and small-launch traffic; no per-seat licensing.
- Keeps **Unity physics as the single simulation** — held/thrown objects and minigames stay exactly as built, with the standard server-authoritative + client-visual-feedback split.

Architecture decisions locked in now (details will live in `Multiplayer.md` when Phase 4 starts):
- **Client-hosted** (host = a player), server-authoritative game logic; no dedicated servers at this scope.
- Note for later: UGS also offers **Distributed Authority** (session-owned authority spread across clients). We're *not* starting there — server-auth is what the course teaches and what our validation model needs — but it's the documented fallback if host-side physics load ever becomes a real problem.
- **All connectivity code goes through one `MultiplayerBootstrap` class.** Nothing else in the game may reference Relay/Sessions/transport types directly. This single seam is what makes the later Steam transport swap (and any service change) a one-file job.
- Minigames run their physics **locally on the interacting client**; the client reports a result summary; the server sanity-checks and applies the outcome. This keeps the tactile feel latency-free.

**Implementation plan (Phase 4 prep):**
1. Add `com.unity.services.multiplayer` when Phase 4 begins; link the Unity Cloud project ID; anonymous auth.
2. Build `MultiplayerBootstrap`: create session (host) / join by code / quick join, exposing plain C# events to the rest of the game.
3. Convert Player → KitchenObject → counters → managers, in that order (per roadmap).
4. Steam decision point at Phase 11: add Facepunch + the community Steam transport behind the same bootstrap; Steam lobby/invites replace join-code UI when running under Steam. Relay/join-code path stays alive as the non-Steam fallback and the editor test path.

---


</details>

<details>
<summary><b>Superseded Decision 3 — Vivox</b></summary>

### Original: Decision 3 — Voice: Vivox (confirmed)

**Choice: Vivox positional voice via UGS. Your instinct is right — this is the clear winner and there's no serious competitor for our requirements.**

Why it fits exactly:
- **3D positional channels are a native feature** — distance attenuation *and* direction come free by feeding each speaker's world position (player head transform) into the channel. Our "can barely hear them across the kitchen" mechanic is configuration, not custom DSP: Vivox channels take audible-distance / conversational-distance / rolloff parameters we tune to ≈ half a kitchen.
- **Free up to 5,000 peak concurrent users** — we will not hit that for a long time.
- Same UGS project + Authentication as our multiplayer sessions; the session join flow and voice join flow share identity.

Implementation plan (Phase 5):
1. Add the Vivox package, enable in UGS dashboard, login alongside session join.
2. One **positional channel per match**; update each participant's 3D position from the player head every ~0.3s (Vivox's tap rate is fine at that cadence).
3. The break-room lobby joins the same kind of positional channel — voice works identically while waiting/practicing.
4. Options: open-mic default with push-to-talk toggle; per-player volume/mute; speaking-indicator UI above heads (Vivox exposes speaking events).
5. VS mode (Phase 9): either one positional channel per team-kitchen, or one shared channel if cross-team trash talk turns out to be fun — playtest call.
6. Design note: diegetic kitchen SFX (timers, sizzle) are deliberately mixed loud enough to *compete* with voice — the audio chaos is a feature. Mixer buses: Voice / SFX / Music.

---


</details>

**Why they were dropped, in one line each:** NGO/Sessions was the right call for a project with no existing netcode — but we *have* existing netcode, on the same Unity version, that already reaches Steam invites; and Vivox's only advantage over Dissonance was first-party integration with the UGS project that Decision 2 just removed the need for.

---

## Sources
- [Unity 6 releases](https://unity.com/releases/unity-6) · [Unity 6.5 release coverage (June 2026)](https://www.cgchannel.com/2026/06/unity-releases-unity-6-5-discover-5-key-features-for-cg-artists/) · [What's new in Unity 6.5 (manual)](https://docs.unity3d.com/6000.5/Documentation/Manual/WhatsNewUnity65.html)
- **The port source of truth:** `C:\Unity\Projects\Extraterrestrial` — `Docs/DECISIONS.md` #3 (netcode stack + the Unity 6.5 patch) and #16 (Dissonance + the vendored shim), `Docs/Ryan/NETWORKING_PLAN.md` (architecture + the two-account test ritual), `Docs/TUNING.md` "Proximity voice" (every tuned value), `CLAUDE.md` (authority invariants + the Steam-missing gotchas)
- [FishNet](https://github.com/FirstGearGames/FishNet) · [FishyFacepunch transport](https://github.com/FirstGearGames/FishyFacepunch) · [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) · [Dissonance Voice Chat](https://placeholder.uk/dissonance/) · [DissonanceVoiceForFishNet (MIT)](https://github.com/LambdaTheDev/DissonanceVoiceForFishNet)
- [Multiplayer Services SDK docs](https://docs.unity.com/en-us/mps-sdk) · [Build a session with Netcode for GameObjects](https://docs.unity.com/en-us/mps-sdk/build-your-first-session) · [NGO releases](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases) *(superseded stack)*
- [Photon Quantum](https://www.photonengine.com/quantum) (deterministic rollback + own physics — the disqualifier for us)
- [Vivox](https://unity.com/products/vivox) · [Vivox pricing FAQ](https://support.unity.com/hc/en-us/articles/31045802890260-Vivox-Pricing-and-Billing-FAQ)
- [Edgar Pro (Asset Store)](https://assetstore.unity.com/packages/tools/utilities/edgar-pro-procedural-dungeon-generator-212735) · [Dungeon Architect](https://dungeonarchitect.dev/unity)
