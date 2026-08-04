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

## Reference footage — Cooking Simulator trailer (`Docs/Videos/CookingSim/`)
`cookingSim.mp4` + frame grids (`CookingSim_grid_01..04.jpg`, 2 fps). Observed sequence: produce rack → tomato carried to board → **knife slicing fully in first person** → blender → pouring liquids between vessels → grilling meat → sauce funneled into cups → potato chopped into chips that scatter across the counter → oven baking → then escalating chaos: prep table swept clear, cardboard boxes ignite, a **flaming gas canister gets carried through the wrecked kitchen**, fire spreads across the floor.

### What Cooking Simulator proves (and we adopt)
1. **No docked views at all** — every action, including knife work, happens in roaming first person: the knife hovers where you aim, a click executes a cut where it hovers, slices are real rigidbodies that fall and scatter. Precision comes from generous *tolerances*, not from a special camera mode.
2. **Liquids as gameplay** — pots/jugs/cups have fill levels; tipping a vessel produces a pour stream that fills whatever is under it. Simplified sim (float fill + stream + trigger), massive immersion payoff. Our steakhouse needs this for sauces/marinades anyway.
3. **Appliances are physical containers** — oven with a door and tray, blender with a lid, each highlighted (outline) when focused. Matches our fridge pattern; extend it everywhere.
4. **Tools are just objects** — knives, tongs, extinguisher, even the gas tank: pick up, use, throw. One grab system covers everything.
5. **Chaos is a *feature with physics rules*, not a fail screen** — fire ignites flammables near heat, spreads to boxes/floor, is fightable (extinguisher), and the kitchen stays wrecked ("you can't turn back time"). Failure is spectacular, legible, and recoverable — not a game-over.
6. **Score lives in the fiction** — "BEAT MY SCORE" sticky notes. Grading presented diegetically (our order tickets can carry the stars).
7. **UI minimalism** — tiny contextual keybind hints only while holding something unusual; brief CANCEL affordance during long actions; outline highlight on the focused appliance.

## Synthesis: the Yes Chef way (decision)
The two references pull in opposite directions and the answer is now clear:

- **Primary = Cooking Simulator's roaming FP direct manipulation.** Everything happens in first person with physical tools and generous tolerances. This preserves our #1 pillar (first-person immersion) and the multiplayer kitchen (you're never locked away from the room).
- **Schedule 1's patterns survive as *techniques*, not as a mode**: ghost targets (translucent outlines on the plate/board showing where things go), one-line contextual step hints, batch rhythm, sub-element chores. Applied inside the FP view.
- **Docked `WorkstationView` is demoted to a *maybe-later assist*** (a lean-in zoom for plating precision or accessibility), only if FP plating tests poorly. Not built in the first pass.

**Difficulty philosophy (explicit, per the dev's call):** low skill floor, high immersion ceiling. Nothing should be *hard to accomplish* — cuts land where you aim with fat tolerances, plating snaps kindly, no action can hard-fail except burning food. The PrepScore rewards care but never gates progress; chaos (dropped food, fires, mess) is the fun consequence layer, always recoverable, never a fail screen.

## Interaction architecture (revised after CookingSim footage)

**One tier: roaming first-person direct manipulation.** E = pickup/place (existing). LMB = use the held tool / work the aimed station (existing `IWorkStation` seam). Holding a tool changes what LMB does; holding nothing keeps today's behavior.

Core mechanic for fine work — the **hover-tool** (the CookingSim knife trick):
1. Player holds a tool (knife) and aims at a valid work surface (board with choppable item).
2. The tool detaches from the hold point and **hovers over the aim point on the surface** — a soft-follow ghost of where the action will land (plus a thin projected guide line for the cut).
3. LMB executes at the hovered position (a quick chop animation + physics cut). Move aim, click again.
4. Look away / release → tool returns to the hand.

This gives Schedule 1's precision *without* leaving first person: the "cursor" is your look direction; the tolerance is authored generously (cut snaps to the nearest sensible plane within ~2–3 cm).

`WorkstationView` docking is **shelved** unless FP plating tests poorly (kept in git history; would return as an optional lean-in assist, not a requirement).

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
- **FP hover-tool:** knife in hand → aim at the board → knife hovers over the aim point with a projected cut-guide line; LMB chops there. Slices are real rigidbodies that tip over and can scatter (board rims/`boardDiscipline` make care matter).
- **Physics v1:** ingredient is pre-authored chunks joined by breakable joints; the chop breaks the joint nearest the guide plane (snap tolerance ~2–3 cm — low skill floor). Real mesh slicing only if chunks feel fake.
- **Metrics:** `evenness` (variance of chunk volumes), `completeness` (cuts made / expected), `boardDiscipline` (nothing knocked off the board).
- Replaces the current LMB-mash on `CuttingCounter`; same `CuttingRecipeSO` data decides inputs/outputs.

### Sauce / marinade brushing & pouring
- **FP hover-tool:** brush/ladle hovers over the aimed surface; hold LMB to paint (raycast splat-map coverage). Finite sauce per scoop — dip to reload (batch rhythm).
- **Liquid vessels (CookingSim tech):** pots/jugs/bottles get a `LiquidContainer` (fill level + liquid type); tilting a held vessel past a threshold emits a pour stream that fills whatever container/surface it hits. Serves marinades, sauces, and future drinks.
- **Metrics:** `coverage`, `containment` (spill outside the target), `thickness` uniformity (v2).

### Plating / assembly (steak dinners)
- **FP with ghost targets:** recipe projects translucent outlines on the plate (steak here, sides there); place/drop components with kind snap-with-tolerance. The Schedule 1 tray→ghost→vessel rhythm, done from first person.
- **Metrics:** `order`/`completeness` (right components), `alignment` (final resting positions vs ghosts).
- If FP precision tests poorly, THIS is the one action that would justify reviving the docked lean-in assist.

### Cooking (stove/oven) — stays roaming, never docks
Real-time and world-side: place, listen (sizzle ramps, beeps), pull at the right moment; pan flip stays a physical FP gesture. Cooking must NOT dock — you watch it *while doing other things*; that tension is the game.
- **Metrics:** `timing` window (already data-driven via chained `CookingRecipeSO`s), `flips` where relevant.
- **Failure:** burned = hard fail (existing Burned Steak chain). Audio is gameplay: timing learnable by ear (Phase 5 diegetic audio).

### The chaos layer (CookingSim's lesson — design now, build incrementally)
Chaos must emerge from the same physics rules as cooking, and always be recoverable:
- **Fire:** a `Flammable` component (ignition threshold, burn duration, spread radius). Heat sources (stove, burning objects) ignite nearby flammables; fire spreads object-to-object and to floor patches; burns out or is extinguished. **Fire extinguisher** = a held tool spraying a suppression cone. First playable slice: pan left too long → grease fire → spreads to an adjacent cardboard box → extinguisher puts it out.
- **Mess:** spills/splats (from the splat-map tech) persist on counters/floor; dropped food stays as clutter (dirty/wasted rule, Phase 3).
- Chaos never ends a run by itself — it costs time, ingredients, and PrepScore, and it makes the post-shift kitchen tell the story of the match.

### Future chores (same patterns, later)
Dish washing (scrub coverage), fridge restock (batch carry), dough rolling (pin motion), rack-of-ribs / tray-of-veg sub-element work (the plant-buds pattern).

## Implementation plan (Phase 2, revised for FP-first)
1. **Held-tool system** ✅ — `Tool` component on ordinary kitchen objects (Knife.prefab + SO; carry/place/drop free via KitchenObject). Holding the knife changes what LMB does.
2. **Hover-tool targeting** ✅ — `PlayerToolUse` on the Player: holding the knife + aiming at a CuttingCounter with a choppable item → the hold point glides (Lerp 14/s) to hover 14cm over the aim point via `PlayerCarry.SetHoldOverride`; a red `LineRenderer` guide marks the cut plane (cut axis = counter local X, aim slides it); LMB runs a dip-chop coroutine and calls `CuttingCounter.ChopAt(t)` with the normalized position. Evenness (1 − normalized variance of slice widths) is computed on completion and logged as a PrepScore preview. Knife blade axis = mesh Z; `Tool.hoverRotationEuler` is the per-model orientation fix-up knob.
3. **Chopping v1**: breakable-joint ingredient (tomato) + chop-at-guide with snap tolerance; slices as rigidbodies; PrepScore stamped on output. **Go/no-go feel test.**
4. **Ghost targets + step hints**: translucent placement outlines (plating first) and the one-line contextual hint UI.
5. **Liquid vessels v1**: `LiquidContainer` fill levels + tilt-to-pour stream + fill detection (sauce pot → ladle → steak).
6. Then sauce brushing (splat-map), plating pass, stove-flip polish.
7. **Chaos slice** (stretch, can slip to Phase 3): `Flammable` + grease fire + extinguisher tool.

## Networking contract (unchanged)
Docked simulation runs locally on the acting client → `{actionType, targetId, metrics, resultState}` → server validates plausibility bounds (reject impossible speeds/counts; accept everything else) → applies outcome → replicates. Spectators see the character at the bench + the result; tool-motion sync is cosmetic, later.

## TODO / open questions
- [x] Docked vs FP chopping — **decided: FP hover-tool** (the CookingSim footage settled it; docking shelved as a plating-only fallback).
- [x] Cooking Simulator reference footage — captured + analyzed above.
- [ ] Hover-tool feel details: does the knife *visibly* leave the hand to hover, or do the FP arms reach with it? (Prototype with a floating knife first; arms come with the Phase 10 rig.)
- [ ] Pan-flip gesture spec (mouse flick vs timed click) — after chopping ships.
- [ ] Multi-grab — v2 QoL for plating several garnishes.
- [ ] `PrepQualitySO` authoring format — define when chopping v1 lands.
- [ ] Fire/chaos scope guardrails — how far does floor-fire spread go before it's over-scoped? Define the cap when the chaos slice starts.
