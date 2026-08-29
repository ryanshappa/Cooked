# Mesh Slicing — dynamic knife cutting

> Status: **design + build plan (Aug 28 2026)**. Replaces the placeholder "squash N times then swap prefab" chop in `CuttingCounter.ChopAt`. Parent design: [`Simulator.md`](Simulator.md) → Chopping. This doc is the source of truth for the cutting feature; update it in the same commit as the code.

## Purpose
When the knife passes through an ingredient, the ingredient's mesh is actually split along the blade's plane into two independent physical pieces, **exactly where the blade went** — slice the left edge of the cheese and only a thin end comes off; slice the middle and you get two halves. Pieces are ordinary `KitchenObject`s (pick up, place, plate, re-cut). Recipe identity ("this is now Sliced Cheese") is derived from what the pieces are, not from swapping in a pre-authored prefab.

This is the Cooking Simulator model. What we know about how they do it (from patch notes / player threads — Big Cheese never published a tech talk):
- Real plane slicing with generated geometry: their 2024 knife rework "calculates product mass using mesh volume, so the weight of each piece matches what you see."
- A red cut-guide line while holding the knife; the cut lands where the line is (we already have this).
- **Minimum-mass gate** per ingredient (2–4 g) stops infinite slicing — the "last piece can't be cut" behaviour players see.
- Known failure mode: very small pieces get flung off the board by the knife collider. We design around it (see §6).
- Cooking Simulator 2 *removed* free cutting for a grid-click menu and reviewers universally called it a loss. Free slicing is identity; keep it.

## Why not the alternatives
| Option | Verdict |
|---|---|
| Pre-authored chunks + breakable joints (the original Simulator.md v1 idea) | ❌ Cuts can only land on authored seams — fails the "cut exactly where I aimed" requirement. |
| Fake slicing (clip-plane shader, vertex push) | ❌ Can't re-cut the pieces; no real physics pieces. |
| **Plane slicing with cap generation** | ✅ Exact, re-cuttable, pieces have real volume/mass. Convex-cap only is fine for our ingredients. |

### Library decision: EzySlice (over Mesh Slicer by Stas Bz)
Decided Aug 28 2026. Reasons: (1) our ingredients are convex and slices of convex solids stay convex, so EzySlice's convex-cap limit never bites; (2) we want two raw meshes we route through `KitchenObject.SpawnSlice` (lineage, mass-from-volume, board-kinematic, NGO plane replication) — Mesh Slicer's spawn-pieces/auto-collider convenience layer would be fought, not used; (3) tiny MIT source we can own and keep deterministic; (4) no per-seat license across Mac/PC. **Revisit** if we need concave ingredients or holes (bell pepper, bagel) or the tomato stem dimple can't be fixed in Blender — the swap is confined to `MeshSlicingService`.

## Current state of the code (what we're replacing)
- `PlayerToolUse.cs` — hover-tool: raycasts (≤1.4 m, layer 6) for a `CuttingCounter` with a choppable occupant, computes `cutT` (0..1 along the item, view-relative axis), positions the knife 14 cm above the item, draws the red guide, and on LMB runs `ChopRoutine` (dip 0.07 s → `counter.ChopAt(cutT)` → raise 0.09 s). **All of this stays** — it's the aiming layer.
- `CuttingCounter.ChopAt(t)` (`CuttingCounter.cs:26-50`) — records `t`, squashes the item, and at `cutsRequired` destroys it and spawns `recipe.output`. **This is what gets replaced.**
  - The "item grows" bug you see: line 38 assigns `localScale` *absolutely* (`1+0.05n, …`) but `Tomato.prefab` root scale is 0.22 and `Cheese.prefab` is 0.7, so the first chop balloons the tomato ~5×. Moot once slicing lands.
- `IWorkStation` has no implementers (dead seam in `PlayerInteract`). Leave it; not part of this work.

### Prerequisites found in the asset audit
- **Every ingredient FBX has Read/Write disabled** (`isReadable: 0` on all of `_Assets/Meshes`, `KitchenObjectsVisuals`, and the Pandazole food set). Slicing reads vertices on the CPU → must enable Read/Write on Cheese, Tomato, Meat (and any future sliceable). Runtime-generated meshes are readable by default, so re-cuts are fine.
- Ingredient prefabs are root (`KitchenObject` + `Rigidbody` + primitive collider, layer 6) → nested visual child (MeshFilter + MeshRenderer, non-unit scale: cheese visual 0.5 under root 0.7; tomato root 0.22). Slicing happens on the **visual's mesh in the visual's transform**, then pieces are re-rooted as new `KitchenObject`s.
- `Cheese.prefab` has a stray `SphereCollider` (leftover) beside its BoxCollider — remove it during this work; it inflates `localCenter`.
- No slicing library installed (`Packages/manifest.json` checked).

## Architecture

```
PlayerToolUse (aim + dip)  ──OnChop(plane)──►  Sliceable.TrySlice(plane)  ──►  MeshSlicingService (EzySlice wrapper, pure)
                                                       │
                                                       ├─► KitchenObject.SpawnSlice(...) ×2   (sanctioned spawn path)
                                                       ├─► SliceLineage updated (root id, volume fractions)
                                                       └─► CuttingCounter / recipe rule re-evaluated → pieces retagged as output SO
```

### Components
| Piece | Responsibility |
|---|---|
| `ThirdParty/EzySlice/` | Vendored MIT library (DavidArayan/ezy-slice). API: `go.Slice(worldPos, worldNormal, capMaterial)` → `SlicedHull` with `upperHull`/`lowerHull` meshes + `CreateUpperHull/LowerHull`. Cap triangulated by monotone chain (convex cross-sections). Multi-submesh aware; cap merged into an existing submesh if `capMaterial` is already on the renderer. |
| `MeshSlicingService` (static) | `bool Slice(MeshFilter source, Plane worldPlane, Material cap, out Mesh a, out Mesh b)`. Pure function over EzySlice so the backend can be swapped (OpenFracture / Stas Bz Mesh Slicer) if we ever need concave ingredients. Also `float Volume(Mesh)` (signed tetrahedra). Deterministic given the same plane → in Phase 4 the server replicates `(origin, normal)` and every client re-derives identical geometry. |
| **`SliceProfileSO`** (data — the feature flag) | `interiorMaterial`, `densityGramsPerCm3`, `minPieceGrams`, and the feel knobs (`cutGap`, `sliceTopKickSpeed`, `chunkScootSpeed`, `siblingIgnoreSeconds`). Assets in `ScriptableObjects/SliceProfiles/`. **A `KitchenObjectSO` with `sliceProfile` set is sliceable; null = not.** Nothing is wired on prefabs. |
| `Sliceable` (runtime component, auto-added) | `KitchenObject.Awake` adds it when its SO `IsSliceable`. Holds only per-piece state (`RootId`, `Generation`, `VolumeFraction`, `Grams`) and reads every tunable from `Profile` (= its SO's profile). `TrySlice(point, normal)` validates (plane intersects; both sides ≥ min mass), slices the visual mesh, spawns two pieces via `KitchenObject.SpawnSlice` (which copy the SO → same profile), kicks the smaller piece, destroys self, fires `OnSliced`. |
| `SliceLineage` (plain class on the `KitchenObject`) | `{ KitchenObjectSO sourceSO; Guid rootId; int generation; float volumeFraction; float grams; List<Plane> cutHistory }`. Volume is conserved through plane cuts so fractions across a lineage sum to 1. |
| `KitchenObject.SpawnSlice(Mesh, Material[], Sliceable template, Transform worldPose, SliceLineage)` | The only path that creates pieces. Builds root (layer 6) + `KitchenObject` + `Rigidbody(mass = grams/1000)` + `MeshCollider{convex=true}` + visual child with the new mesh + copied `Sliceable`. |
| `CuttingRecipeSO` (extended) | Today: `input, output, cutsRequired`. New: `minPieces`, `maxPieceVolumeFraction` (e.g. SlicedCheese: ≥4 pieces, none > 0.35). Rule is a predicate over the lineage; when it becomes true, every piece in the lineage swaps its `KitchenObjectSO` to `output` (meshes untouched). `cutsRequired` kept only as a fallback/derived value. |
| `CuttingCounter` | Stops owning cut state. Becomes: "is the occupant `Sliceable` with a recipe?" for hover gating, plus the recipe-rule evaluation after each slice and the board-discipline metric (pieces still on the board). |

### Cut plane — from blade geometry, not velocity
The VR/Valem recipe (`normal = cross(tip − heel, velocity)`) is right for a swung sword and wrong for us: our knife is held in a known hover pose and a straight chop has ~zero lateral velocity → noisy normal. Instead the knife's `Tool` gets two authored transforms, `bladeHeel` and `bladeTip`, and the blade plane is:

```
edge   = tip − heel                (world)
normal = cross(edge, bladeUp)       (bladeUp = knife's spine direction, world)
origin = heel
```

Exact for a straight chef's knife regardless of how the player yawed the wrist. The guide line already lies in this plane (it spans `lineDir`, which is the knife's forward) — the guide is literally the plane ∩ item-top, so WYSIWYG holds.

### Cut trigger — "blade reached the board"
Options considered: trigger enter/exit with opposite-side check, `OnTriggerExit` + overlap, swept AABB, velocity gate. **Chosen: deterministic dip completion.** `ChopRoutine` already dips the knife; the cut fires when the dip reaches board height *and* the plane still intersects the item's bounds. Frame-rate independent, no trigger-callback ordering issues, and no "poked it and pulled back" false cuts. A blade that never reaches the board = no cut (Cooking Sim behaviour). Partial cuts (notches) are a boolean op, not a plane slice — deliberately unsupported.

Later (free-hand/VR-style motion cutting) can add an enter/exit detector that feeds the same `OnChop(plane)` event; nothing downstream changes.

### Where the plane goes today
`PlayerToolUse` currently computes `cutT` and the knife hover pose. The change: instead of `counter.ChopAt(cutT)` it builds the world plane from the blade transforms at the bottom of the dip and calls `sliceable.TrySlice(plane)`. `cutT` remains useful for the guide and for snapping (see tolerance).

### Tolerance / skill floor (from Simulator.md)
"Cuts land where you aim with fat tolerances." Implementation: none needed for correctness — the plane is exact. Optional helper: if a cut would produce a piece under `minPieceGrams` but > 50 % of it, snap the plane inward to the minimum; otherwise refuse and flash the guide. Decide in playtest.

## Physics rules for pieces
- Pieces spawn **exactly where the parent was** (same world pose; EzySlice keeps vertices in parent local space). No explosion: offset each hull ±(bladeThickness/2 ≈ 1 mm) along the normal, and `Physics.IgnoreCollision` between the two siblings for 0.2 s so PhysX depenetration doesn't launch them.
- Convex `MeshCollider` — PhysX 255-tri cap; Unity auto-simplifies, so food meshes (≤2 k tris) are fine. Fallback to BoxCollider if cooking fails.
- Kinematic while on the board (existing `KitchenObject` on-surface state), dynamic when knocked loose. The **board becomes a multi-slot surface**: `CuttingCounter` must hold N pieces, not one `IKitchenObjectParent` slot (KitchenTable's multi-slot pattern is the precedent). Pieces rest on the board by physics, not by anchoring to `counterTopPoint`.
- Knife collider must not shove pieces: during hover/dip the knife's colliders are already off (held state). Keep it that way.
- Mass from volume × density (cheese ≈ 1.1, tomato ≈ 1.0, beef ≈ 1.05 g/cm³ — item scale is slightly larger than real, that's fine).

## Metrics (feeds `PrepScore`)
All derived from data the slicer already has:
- `evenness` = 1 − coefficient of variation of piece volumes in the lineage (replaces today's width-variance metric).
- `completeness` = recipe predicate satisfied (pieces ≥ `minPieces`).
- `boardDiscipline` = fraction of pieces still on the board when the player walks away / picks up.
- (v2) `straightness` = parallelism of successive cut normals.
Cooking Simulator shows a "this piece will be N g" preview on the guide line — we can compute both sides' volume *before* cutting from the plane; cheap, and worth doing once the feel is good.

## Networking contract (Phase 4)
Slice runs locally on the acting client; the client sends `{targetId, planeOrigin, planeNormal}`; server validates (plane intersects bounds, min-mass rule, piece cap) and re-runs the deterministic slice; all clients derive identical meshes from the same plane. No vertex data on the wire. Piece identity = `rootId + generation index`.

## Build plan (piece by piece)
Each step is one sitting and leaves the game playable.

1. **Vendor EzySlice + Read/Write.** *(landed Aug 28 2026 — `Assets/_Assets/ThirdParty/EzySlice/`, Read/Write on Cheese block / Tomato / Food_Steak / Food_Cooked Steak, stray cheese SphereCollider removed, editor smoke test in `Assets/_Assets/Editor/SliceTestMenu.cs` → menu **Yes Chef ▸ Slicing**.)* Copy `EzySlice/` into `Assets/_Assets/ThirdParty/EzySlice/` (MIT notice kept). Enable Read/Write on `Cheese block.fbx`, `Tomato.fbx`, Pandazole `Food_Steak`. Check the tomato mesh is closed/convex (stem dimple → fix in Blender via MCP if EzySlice leaves a hole). Remove the stray cheese SphereCollider. ✅ criteria: an editor test slices the cheese visual with a hard-coded plane and both hulls render with a cap.
2. **`MeshSlicingService` + `Sliceable` + `KitchenObject.SpawnSlice`.** *(landed Aug 28 2026 — see Wiring below; verified in play mode via code: cheese on a board → 30 % cut → 419 g + 460 g pieces at rest, re-cut → 237 g + 223 g, no physics pop. Feel test with the real knife still to be played.)* Cheese only. Cut plane from `bladeHeel/bladeTip` on the Knife prefab; `PlayerToolUse` calls `TrySlice` at dip bottom instead of `ChopAt`. Pieces spawn in place, kinematic on the board, sibling-collision ignored briefly. Interior material: flat cheese-yellow first. **Go/no-go feel test** — does "cut where I aim" feel right?
3. **Multi-piece board.** `CuttingCounter` holds a list; pieces rest by physics; pickup of an individual piece works via existing `PlayerInteract` (pierce-through targeting already handles surfaces). Hover gating targets the specific piece under the aim, not "the occupant".
4. **Lineage + recipe rule.** `SliceLineage`, extended `CuttingRecipeSO` (`minPieces`, `maxPieceVolumeFraction`), SO retag when satisfied. Min-mass gate + piece cap. Delete the squash code and `cutsRequired` usage. Evenness/completeness logged as PrepScore preview.
5. **Tomato + steak.** *(Tomato landed Aug 28 2026 via the profile flag — verified: 935 g tomato → 750 + 128 + 57 g, round slices with flesh caps, end piece lands cap-down. Steak pending.)* Interior materials (tomato flesh, raw/cooked steak cross-section — cook state drives the cap material, so it's a `KitchenObjectSO`/cook-state property, not a slicer property). Densities tuned.
6. **Polish.** Chop SFX (`SFX_chop01-03.wav` exist, unused), tiny cut VFX, weight preview on the guide, tolerance snap decision, plate acceptance of sliced pieces.

## Scene / Inspector wiring (as of step 2)
- **Scripts:** `MeshSlicingService.cs` (static EzySlice wrapper + volume), `Sliceable.cs` (on ingredient roots; `interiorMaterial`, `densityGramsPerCm3`, `minPieceGrams`, `siblingIgnoreSeconds`; runtime lineage `RootId/Generation/VolumeFraction/Grams`), `KitchenObject.SpawnSlice(...)` (builds a piece root: MeshFilter+MeshRenderer+convex MeshCollider+Rigidbody+Sliceable+KitchenObject at the source visual's world pose, layer copied, SO copied), `PlayerToolUse` (hover now resolves a `Sliceable` — the board's slotted occupant *or* a loose piece resting on a board that `CanCut` it — and `ChopRoutine` calls `TrySlice(cutPointWorld, cutNormal)` at the bottom of the dip; `cutNormal` = flattened camera right, i.e. the guide line's plane), `CuttingCounter` (squash/`ChopAt`/evenness removed; `CanCut(ko)`, `HasChoppableOccupant()` now also requires `Sliceable`).
- **Cut plane** = point under the red guide at the item's top + normal = flattened camera-right. The knife's hover rotation is `LookRotation(lineDir)` so its blade lies in this plane — WYSIWYG with the guide. (Blade heel/tip transforms deferred: not needed while the plane comes from the aim math.)
- **Making a food sliceable (the whole recipe):** (1) enable Read/Write on its mesh; (2) create a `SliceProfileSO` (Create ▸ Scriptable Objects ▸ SliceProfileSO) with an interior material; (3) assign it to the food's `KitchenObjectSO.sliceProfile`. Done — no prefab changes. Current: `SliceProfile_Cheese` (CheeseInterior.mat, 1.1 g/cm³, min 2 g) on Cheese, `SliceProfile_Tomato` (TomatoInterior.mat, 1.0 g/cm³, min 2 g) on Tomato. Meat has no profile yet (step 5: needs a raw/cooked interior look).
- **Cheese.prefab:** stray SphereCollider removed; no slicing components on it (runtime-added). Read/Write is on for `Cheese block.fbx`, `Tomato.fbx`, `Food_Steak.fbx`, `Food_Cooked Steak.fbx`.
- **Hover tolerance:** aiming at the board within `nearPieceRadius` (5 cm) of a loose piece targets that piece — needed for round foods (a tomato's top edge overhangs empty board, so the ray hits wood).
- **Editor test:** menu **Yes Chef ▸ Slicing** (`Assets/_Assets/Editor/SliceTestMenu.cs`).
- Pieces are **loose** physics objects (no `IKitchenObjectParent`); they can be picked up like any floor item, and placed back on a board (their SO is still Cheese, so the recipe accepts them).
- **Separation feel** (`Sliceable`: `cutGap` 4 mm, `sliceTopKickSpeed` 1.0 m/s, `chunkScootSpeed` 0.12 m/s): the smaller piece is pushed at its **top edge** away from the bigger one (COM velocity = half the top speed, angular velocity = topSpeed/height) so it pivots on its base and flops over — a thin slice lands flat next to the block; successive slices domino onto each other (Cooking Simulator look). Chunky pieces (thickness > 60 % of height) just scoot. The block itself is never kicked. Lessons: a spin about the centre of mass can't tip a standing slice (gravity's restoring torque wins, and Unity caps angular velocity at 7 rad/s by default — pieces get `maxAngularVelocity = 60`); and the spin sign was verified empirically per physics step (Unity is left-handed) — the wrong sign silently fights the slide and the slice just stands there.
- **Min-mass gate** is 2 g on cheese and tomato (5 g refused legit 1 cm slivers at the wedge's thin end).
- **CuttingCounter.prefab:** the `Chopping Board` visual child now has its own BoxCollider (layer Interactable). Without it, loose pieces fell to the counter's collider top (0.91) and sank 2 cm into the board mesh (top 0.931).
- **Editor gotcha:** with the editor unfocused, play mode doesn't advance (`Run In Background` off) — MCP-driven physics tests must set `Application.runInBackground = true` first. Cutting the same lineage several times in one frame also misbehaves (the previous generation is destroyed end-of-frame) — never a real-play case, but don't do it in tests.

## Known issues / TODO
- [ ] EzySlice caps are convex-only — fine for cheese/tomato/steak; concave ingredients (bell pepper, whole chicken) need a backend swap behind `MeshSlicingService`.
- [ ] Duplicate vertices after slicing inflate collider cooking (EzySlice issue #13) — weld if it shows up in the profiler.
- [ ] Destroy intermediate `Mesh` objects on re-cut or they leak.
- [ ] `Simulator.md` still says "breakable-joint ingredient" for Chopping v1 and documents a "staple" guide + knife held pose (0,−30,0) that the prefab doesn't have (both offsets are zero). Reconcile when step 2 lands.

## Sources
- Cooking Simulator patch notes (mesh-volume mass, min-weight thresholds, auto-cutter): https://steamcommunity.com/app/641320/allnews/?l=english · cutting preview thread: https://steamcommunity.com/app/641320/discussions/0/3153076876630953682/ · portion mechanics: https://www.gamepressure.com/cooking-simulator/accurate-handling-and-cutting-mode/zfc5e2
- Cooking Simulator 2 dropped free cutting: https://www.neonlightsmedia.com/blog/cooking-simulator-2-review
- Metal Gear Rising slicing analysis (guide lines, debris cap): https://simonschreibt.de/gat/metal-gear-rising-slicing/
- Algorithm walkthrough: https://medium.com/@hesmeron/mesh-slicing-in-unity-740b21ffdf84 · ear clipping: https://www.habrador.com/tutorials/math/10-triangulation/
- EzySlice (MIT): https://github.com/DavidArayan/ezy-slice · OpenFracture (MIT, concave, Delaunay caps): https://github.com/dgreenheck/OpenFracture · Mesh Slicer (Stas Bz, $50, Unity 6): https://assetstore.unity.com/packages/tools/modeling/mesh-slicer-59618 · Dynamic Mesh Cutter ($17.50, async): https://assetstore.unity.com/packages/tools/modeling/dynamic-mesh-cutter-208384
- VR blade recipe (Valem / VelocityEstimator): https://www.patreon.com/posts/how-to-slice-in-81933161 · pass-through detection: https://discussions.unity.com/t/detect-object-slicing-passing-through-another-object-completely/921898
- Read/Write + MeshCollider: https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html · convex 255-tri limit: https://discussions.unity.com/t/really-confused-about-convex-mesh-collider-triangle-limit/662347 · mesh volume: https://gist.github.com/unitycoder/379e885c9215d48bcfb5c554e13a5d26
