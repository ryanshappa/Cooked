# Yes Chef — Technical Plan & Architecture Decisions

This document records the big technical decisions for the project — what we chose, what we considered, and why — plus the implementation plan for each. The task-level roadmap lives in [`../CLAUDE.md`](../CLAUDE.md); this is the "why" behind it. Last reviewed: **August 2026**.

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

## Decision 2 — Multiplayer: Netcode for GameObjects + Unity Multiplayer Services SDK (Sessions), Steamworks later as a transport swap

**Choice: NGO 2.x (client-hosted, server-authoritative) + `com.unity.services.multiplayer` (the Sessions API, which bundles Auth/Lobby/Relay) for connectivity. Facepunch Steamworks enters later, at Steam-release time, as an alternate transport + platform layer — not as the netcode. Photon Quantum: no.**

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
4. Steam decision point at Phase 11: add Facepunch + Steam transport behind the same bootstrap; Steam lobby/invites replace join-code UI when running under Steam.

---

## Decision 3 — Voice: Vivox (confirmed)

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
- **Phase 4** uses the Sessions API path (Decision 2) — simpler than the older Lobby+Relay wiring the course shows, same concepts.
- **Phase 5** is Vivox as confirmed (Decision 3).
- **Phase 6** is the custom generator (Decision 4).
- **Phase 10–11** carry the asset swap (Decision 5) and the Steam/Facepunch decision point.

## Sources
- [Unity 6 releases](https://unity.com/releases/unity-6) · [Unity 6.5 release coverage (June 2026)](https://www.cgchannel.com/2026/06/unity-releases-unity-6-5-discover-5-key-features-for-cg-artists/) · [What's new in Unity 6.5 (manual)](https://docs.unity3d.com/6000.5/Documentation/Manual/WhatsNewUnity65.html)
- [Multiplayer Services SDK docs](https://docs.unity.com/en-us/mps-sdk) · [Build a session with Netcode for GameObjects](https://docs.unity.com/en-us/mps-sdk/build-your-first-session) · [NGO releases](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases)
- [Photon Quantum](https://www.photonengine.com/quantum) (deterministic rollback + own physics — the disqualifier for us)
- [Vivox](https://unity.com/products/vivox) · [Vivox pricing FAQ](https://support.unity.com/hc/en-us/articles/31045802890260-Vivox-Pricing-and-Billing-FAQ)
- [Edgar Pro (Asset Store)](https://assetstore.unity.com/packages/tools/utilities/edgar-pro-procedural-dungeon-generator-212735) · [Dungeon Architect](https://dungeonarchitect.dev/unity)
