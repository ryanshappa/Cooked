# Physics Cooking Minigames & Prep Grading

## Purpose
This is the game's identity pillar: cooking actions are hands-on physics "chores" (à la Schedule 1), not fill-a-bar waits — and every action is **graded**. Slices can be even or ragged, sauce coverage can be full or patchy, cook timing can be nailed or blown, and that quality flows into the order payout. The skill ceiling is what makes runs replayable; the grading is what makes skill visible.

Status: **design doc** (Phase 2 builds this). Each minigame section will grow implementation notes as it's built.

## Design principles
1. **Physically honest** — every grade derives from something measurable in the actual simulation (slice sizes, splat coverage, timer windows, stack alignment). No hidden dice, no grading a thing the player couldn't see and control.
2. **Fast feedback** — the grade appears the moment the action completes (stars/sparkle + numeric under the hood), so players connect cause → effect and want to retry. The break room's practice stations (Phase 7) exist precisely so this loop is learnable outside match pressure.
3. **Forgiving floor, high ceiling** — a sloppy action still *counts* (the order can be delivered), it just pays less. Failure states (burned, dropped on floor) are the only hard fails. Learnable in <1 minute, masterable over many runs.
4. **Local feel, server truth** — the physics minigame always runs on the interacting player's client (zero-latency feel). The client reports a compact result (metrics, not physics state); the server bounds-checks it for plausibility and applies the outcome. Nothing mid-minigame is networked.
5. **Grades roll up** — each prepped component carries its `PrepScore`; a delivered order's payout = base reward × quality multiplier from its components' scores (+ delivery-time bonus). Defined in OrdersAndScoring.md when Phase 3 lands.

## The grading framework (`PrepScore`)
Every gradeable action produces a **PrepScore 0–100** computed from 2–3 named metrics, each 0–1:

```
PrepScore = 100 × Σ (weightᵢ × metricᵢ)      (weights per action type, sum to 1)
```

- Metrics live on the resulting KitchenObject (e.g. a `PreparedComponent` carrying `{actionType, metrics[], score}`) so plates/orders can aggregate them later.
- Display tiers: ★☆☆ <50 "Sloppy", ★★☆ 50–85 "Good", ★★★ >85 "Chef's kiss". Tuning of tier cuts and weights is per-action data (ScriptableObject), not code.
- Server validation rule of thumb: reject/clamp results that are physically implausible (e.g. 100% coverage in <0.5s, more slices than the ingredient allows), accept everything else. We're preventing cheating-by-packet, not litigating skill.

## Minigames

### Chopping
- **Verb:** hold knife (held-tool system), swing/press through the ingredient on a cutting board.
- **Physics:** ingredient starts as pre-authored chunks held by breakable joints (v1); knife contact + downward force breaks joints along the contact plane. Real mesh slicing is a v2-maybe, only if chunks feel fake.
- **Metrics:** `evenness` (1 − normalized variance of chunk volumes), `completeness` (cuts made / cuts expected), optional `boardDiscipline` (pieces still on the board, none on the floor).
- **Failure:** pieces knocked to the floor are lost (trash or 5-second-rule design call — TBD).
- **Open questions:** free swing vs constrained-to-plane guide; knife as physics object vs animated with physics query.

### Sauce/marinade brushing (steak marinade, garlic butter, condiments)
- **Verb:** hold brush/ladle/squeeze bottle, move over the steak/bread/plate to paint.
- **Physics/tech:** coverage splat-map on the target surface (raycast decal painting); sauce amount is finite per scoop.
- **Metrics:** `coverage` (% of valid zone painted), `containment` (1 − % painted outside the zone / spilled), `thickness` uniformity (v2).
- **Failure:** none hard — just low score; spilling sauce on the counter leaves a mess (cleaning = future chore idea).

### Cheese & toppings placement
- **Verb:** sprinkle cheese (particle-ish pinch-and-release) / place toppings by hand.
- **Metrics:** `coverage`/`distribution` (toppings spread across the surface, e.g. mean nearest-neighbor distance vs ideal), `count` (right number of pepperoni for the recipe).
- **Failure:** toppings on the floor are lost.

### Cooking (stove/oven/fryer)
- **Verb:** put the thing in/on, watch and listen (sizzle pitch, smoke), pull it at the right moment; flip where relevant (pan flip gesture for patties).
- **Physics:** pan/tray is a real container; the flip is a genuine impulse + catch.
- **Metrics:** `timing` (1 at window center, falling to 0 at raw/burned edges), `flips` (patty: both sides in window), per-item cook state is continuous, not stepped.
- **Failure:** **burned is a hard fail** (item → trash); dropped-the-flip means pick it up off the floor (lost) or the stovetop (recoverable, small penalty).
- **Audio is gameplay:** timing is learnable by ear (sizzle ramps, beeper) — feeds the Phase 5 diegetic-audio design.

### Assembly / plating (steak dinners)
- **Verb:** physically arrange components on the plate (steak, sides, garnish); snap-with-tolerance (magnetic assist inside a tolerance radius so it's tactile, not rage-inducing).
- **Metrics:** `order` (components in recipe order), `alignment` (stack centered/straight — measured from final resting transforms), `completeness` (all required components).
- **Failure:** wrong-order stack must be un-stacked (components come apart), toppling a stack scatters it.

## Networking summary (Phase 4 contract)
Client runs the minigame → sends `{actionType, targetObjectId, metrics, resultState}` → server validates plausibility + applies (transforms ingredient, stamps PrepScore) → replicates result to all. Spectating players see the *animation* of the act (tool motion sync is cosmetic) but never simulate it.

## TODO
- [ ] Prototype chopping v1 and sauce v1 first — they stress the two core techs (breakable-joint slicing, splat-map painting); go/no-go on feel before building the rest.
- [ ] `PrepQualitySO` per action type: metric weights, tier cutoffs, plausibility bounds.
- [ ] Decide held-tool interaction model (tool in hand replaces carry slot? separate tool slot?) — prereq for all of these.
- [ ] Playtest grading visibility: is a star popup enough, or do we need a per-metric breakdown on the practice-station scoreboard?
