# The Simulator System (cooking actions)

> Renamed from `Minigames.md` (Aug 2026). "Minigame" was the wrong frame: these aren't detours from the game — they ARE the game. The right model is a **simulator** (Cooking Simulator, Schedule 1): dynamic, physical chores that feel like genuinely *doing the work*, graded on how well you do it.

## Purpose
Yes Chef's identity pillar: cooking actions are hands-on physical tasks — chop, sear, brush, plate — performed with real physics and direct manipulation, not bars or QTEs. Immersion comes from everything being dynamic: items are objects, tools are tools, and each chore is a small piece of real work in the flow of a restaurant shift.

## Reference footage — Schedule 1 (`Docs/Videos/Schedule1/`)
`Schedule 1.mp4` + frame grids (`Schedule1_grid_01..03.jpg`, 2 fps, read left→right, top→bottom). Two actions captured:

**A. Plant trimming/harvest (grid 1, rows 1–4):** first-person, aiming at *individual buds on one large plant*, working around it piece by piece. Pattern: a big object composed of many small sub-interactables; the chore is visiting each one deliberately.

**B. Packaging station (grids 1 row 4 → grid 3):** the load-bearing reference. Observed structure:
1. Approach bench in first person → "Use Packaging Station" prompt → interact.
2. **Camera docks into a fixed bench view** — movement locks, mouse cursor frees. The station is a little diorama you lean into.
3. Product appears as **loose physical nuggets on a tray** (they roll, they scatter).
4. A **translucent ghost baggie** shows where product goes; a one-line **step prompt** sits at top center and changes as the task advances: *"Insert product + packaging"* → *"Place product into packaging"* → *"Place packaging in hopper."*
5. Player **drags nuggets one at a time with the cursor** (bottom-left hints: Interact/Drag, Multi-grab) into the baggie — real dragging with slight physics, misses are possible.
6. Filled baggie is itself dragged to the **hopper** (chute in the bench).
7. In-world **PACK** button on the bench advances the batch; **Exit** undocks back to first person.

### What makes it feel good (design principles to steal)
1. **Dock-in station view** for fine work — solves first-person precision without leaving the fiction. The bench is the UI.
2. **Direct manipulation** — the cursor grabs *objects*, never abstract UI. Every item is physical the whole way through.
3. **Ghost targets** — translucent outlines show where things go; no text needed to explain placement.
4. **One-line step prompts** — the only text UI, contextual, always current. (This is where prompt text returns to Yes Chef — inside station views only; the roaming HUD stays clean.)
5. **Batch rhythm** — tray of N items → repeat the small motion N times → satisfying completion beat (hopper swallow / PACK).
6. **Sub-element chores** — one big thing made of many small targets (the plant's buds ≈ our pizza's topping spots, a rack of ribs, a tray of veg).

## The two interaction tiers (proposed architecture)

| Tier | Where | Camera | Input | Used for |
|---|---|---|---|---|
| **Coarse (roaming)** | anywhere in kitchen | normal FP | E pickup/place, LMB work | carrying, placing, tossing, stove flips, oven loading — everything built so far |
| **Fine (docked)** | at a `WorkstationView` station | camera glides to fixed bench view; movement locked, cursor freed | cursor drag/click on physical items; Esc/E to undock | chopping precision, sauce/marinade brushing, plating/assembly, packaging-style batch work |

Docking is per-station data (camera anchor transform + allowed actions on the counter). A docked player's character stays leaning at the bench in the world (matters for multiplayer visibility later; a docked player occupies that station).

**Open question (prototype answers it):** does chopping live in the docked tier (Schedule 1 precision) or the roaming tier (Cooking Simulator holds the knife in FP)? Instinct: prototype **docked first** — precise, readable, easier to make feel good — with FP knife-in-hand as a stretch experiment. The two tiers share all underlying physics either way.

## Grading (`PrepScore`) — unchanged framework
Every gradeable action produces a **PrepScore 0–100** from 2–3 physically measured metrics, weights/tiers per action in `PrepQualitySO` data:

```
PrepScore = 100 × Σ (weightᵢ × metricᵢ)      (weights per action type, sum to 1)
```

- Metrics live on the resulting KitchenObject (a `PreparedComponent` carrying `{actionType, metrics[], score}`) so plates/orders aggregate them later.
- Display tiers: ★☆☆ <50 "Sloppy", ★★☆ 50–85 "Good", ★★★ >85 "Chef's kiss".
- **Physically honest**: every metric derives from the sim (slice sizes, splat coverage, timing windows, resting positions) — no hidden dice.
- **Fast feedback**: grade appears the moment the action completes; the break room's practice stations (Phase 7) make the loop learnable outside match pressure.
- **Forgiving floor, high ceiling**: sloppy work still counts, pays less; only burned/dropped hard-fails.
- Scores roll into order payout (Phase 3): base reward × quality multiplier (+ delivery-time bonus).

## Actions (updated to the simulator model)

### Chopping (cutting board) — FIRST PROTOTYPE
- **Docked view:** overhead-ish board view. Ingredient sits on the board physically; knife follows the cursor with a constrained motion (lift → position → slice-drag = one cut).
- **Physics v1:** ingredient is pre-authored chunks joined by breakable joints; a slice-drag through the body breaks along the nearest joint plane. Cut placement decides evenness. Real mesh slicing only if chunks feel fake.
- **Metrics:** `evenness` (variance of chunk volumes), `completeness` (cuts made / expected), `boardDiscipline` (nothing knocked off the board).
- Replaces the current LMB-mash on `CuttingCounter`; same `CuttingRecipeSO` data decides inputs/outputs.

### Sauce / marinade brushing
- **Docked view:** steak/bread/plate on the bench; brush or ladle follows the cursor; painting = raycast splat-map coverage while held over the surface. Finite sauce per scoop — dip to reload (the batch rhythm).
- **Metrics:** `coverage`, `containment` (spill outside the target), `thickness` uniformity (v2).

### Plating / assembly (steak dinners)
- **Docked view:** plate centered; components on side trays — exactly the packaging-station layout (tray → ghost target → plate). Ghost outlines mark steak/sides positions per recipe; drag with snap-with-tolerance.
- **Metrics:** `order`/`completeness` (right components), `alignment` (final resting positions vs ghosts).

### Cooking (stove/oven) — stays roaming, never docks
Real-time and world-side: place, listen (sizzle ramps, beeps), pull at the right moment; pan flip stays a physical FP gesture. Cooking must NOT dock — you watch it *while doing other things*; that tension is the game.
- **Metrics:** `timing` window (already data-driven via chained `CookingRecipeSO`s), `flips` where relevant.
- **Failure:** burned = hard fail (existing Burned Steak chain). Audio is gameplay: timing learnable by ear (Phase 5 diegetic audio).

### Future chores (same patterns, later)
Dish washing (docked scrub coverage), fridge restock (batch carry), dough rolling (docked pin motion), rack-of-ribs / tray-of-veg sub-element work (the plant-buds pattern).

## Implementation plan (Phase 2, reordered around docking)
1. **`WorkstationView` + docking**: camera glide to a bench anchor (Cinemachine priority swap), movement/input mode switch (`GameInput.SetPlayerInputActive(false)` + cursor unlock), Esc/E undocks, character stays at bench. Prototype on a CuttingCounter.
2. **Cursor physics-drag** inside the docked view: grab/hold/release rigidbodies with the mouse (spring-joint drag — the Schedule 1 nugget feel) + ghost-target snap zones.
3. **Chopping v1** in the docked view (knife-follow + breakable-joint slicing), PrepScore stamped on the output. **Go/no-go feel test.**
4. **Step-prompt UI** for docked views only (top-center one-liner, per-station step machine).
5. Then sauce brushing (splat-map tech), then plating (reuses drag + ghosts wholesale), then stove-flip polish.

## Networking contract (unchanged)
Docked simulation runs locally on the acting client → `{actionType, targetId, metrics, resultState}` → server validates plausibility bounds (reject impossible speeds/counts; accept everything else) → applies outcome → replicates. Spectators see the character at the bench + the result; tool-motion sync is cosmetic, later.

## TODO / open questions
- [ ] Docked vs FP chopping — answered by prototype step 3.
- [ ] **Cooking Simulator reference footage wanted** (knife/pan handling fully in FP) — it's the counter-example to Schedule 1's docked pattern; would settle the chopping question with evidence. Same capture pipeline: video → `ffmpeg -vf "fps=2,scale=480:270,tile=6x6"` grids.
- [ ] Multi-grab (Schedule 1 has it) — probably v2 QoL for plating several garnishes.
- [ ] Does docking auto-place the held item onto the bench input zone? (Probably yes.)
- [ ] `PrepQualitySO` authoring format — define when chopping v1 lands.
