# Yes Chef — System Documentation

This folder is the shared brain for development across machines (Mac ↔ PC). Every gameplay system gets its own markdown file here, updated **in the same commit** as the code it describes. If a doc and the code disagree, the code wins — then fix the doc.

The overall project plan/roadmap lives in [`../CLAUDE.md`](../CLAUDE.md). The big architecture decisions (Unity version, multiplayer stack, voice, procedural generation, asset strategy) — with the options considered and reasoning — live in [PLAN.md](PLAN.md).

## Doc format

Each system doc covers:
1. **Purpose** — what the system does in one paragraph.
2. **Files** — scripts/SOs/prefabs involved.
3. **How it works** — the flow, key decisions, and any non-obvious behavior.
4. **Scene/Inspector wiring** — what must be hooked up for it to function (critical for reproducing setups on the other machine).
5. **Known issues / TODO** — debt and planned changes.

## Index

### Written (systems that exist)
- [PLAN.md](PLAN.md) — architecture decisions & technical plan (Unity 6.5, NGO + Sessions, Vivox, custom kitchen generator, asset strategy)
- [PlayerController.md](PlayerController.md) — first-person movement, collision, camera rig
- [InputSystem.md](InputSystem.md) — Input System actions + `GameInput` wrapper
- [InteractionSystem.md](InteractionSystem.md) — look-at raycasting, interact prompts, pickup/carry/drop
- [KitchenObjectSystem.md](KitchenObjectSystem.md) — kitchen objects, parents/surfaces, `KitchenObjectSO` data
- [SceneSetup.md](SceneSetup.md) — current test scene contents and wiring
- [Minigames.md](Minigames.md) — physics cooking minigames + PrepScore grading framework (design doc; Phase 2 builds it)
- [Counters.md](Counters.md) — BaseCounter + station catalog (ClearCounter, ContainerCounter/fridge)

### Planned (create when the system is built)
- OrdersAndScoring.md — recipes, order manager, scoring/money (Phase 3)
- GameLoop.md — GameManager state machine, modes (Phases 3 & 9)
- Multiplayer.md — NGO architecture, authority model, UGS Lobby/Relay (Phase 4)
- VoiceAndAudio.md — Vivox proximity chat, SFX/mixer design (Phase 5)
- KitchenGeneration.md — procedural layout rules and generator (Phase 6)
- BreakRoom.md — physical lobby, ready-up, practice stations (Phase 7)
- Progression.md — money persistence, cosmetics, shop (Phase 8)
- ArtDirection.md — look, asset pack audit, character/animation pipeline (Phase 10)
- Audio.md — music tracks and sourcing decisions (Phase 5/10)
