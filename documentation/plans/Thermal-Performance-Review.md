# Thermal performance editing: greenfield UX and architecture review (1 Oct 2026)

Status: **review accepted by the owner, with the decisions below**. No code is changed by this document. It is the
follow-up to the U-value workflow ([UValue-Workflow-PLAN.md](UValue-Workflow-PLAN.md); records
[UValue-LayerPicker-PR1.md](../UValue-LayerPicker-PR1.md), [UValue-SetUValue-PR2A.md](../UValue-SetUValue-PR2A.md),
[UValue-Window-PR2B.md](../UValue-Window-PR2B.md), [UValue-Glazing-PR3.md](../UValue-Glazing-PR3.md)) and **replaces
the optional PR4** in that plan.

## Question

If SAM's historical thermal-editing UI did not exist, but today's engine did, what is the simplest, safest and most
intuitive way for a building-performance engineer to inspect, change, compare, apply, undo and validate the thermal
performance of walls, roofs, floors, windows and doors? The design starts from user intent, not existing windows,
and aims for the minimum cognitive load and the fewest meaningful interactions.

## Owner decisions (1 Oct 2026)

1. **Docked panel is the target architecture.** Start with a small feasibility spike, because SAM has no docking
   infrastructure. If docking is too invasive, host the same control and view-model in a modeless tool window for the
   time being.
2. **Report and change log.** Move to one append-only thermal change log per model, but not in the first stages. Keep
   the existing report generation until the panel architecture is stable.
3. **Classic calculators.** Keep them through Stages B, C and D, demoted to **Legacy**. Remove them only after explicit
   parity testing proves that the panel covers their useful workflows.
4. **Stage A becomes A-lite.** It contains only changes that survive into the new architecture. Legacy windows are not
   polished.

## Facts that shaped the review

- The 3D context menu offers three construction verbs for panels (Assign Construction, Assign Construction By UValue,
  Set U-value...) and three for apertures (Assign Aperture Construction, By gValue, Set glazing...)
  (`WPF/SAM.Analytical.UI.WPF/Windows/AnalyticalWindow.xaml.cs`, `ViewControl_ObjectContextMenuOpening`).
- SAM has no persistent properties or inspector panel. "Properties (P)" opens a modal editor for one object. The main
  layout is a tree, a splitter and the view tabs.
- Both engines already have a pure model-level core,
  `internal static AnalyticalModel SetUValue(this AnalyticalModel, SetUValueRequest, out SetUValueResult)` and
  `SetGlazing(this AnalyticalModel, ...)`. Only the UI wrappers call `SetJSAMObject`, so several changes can be composed
  on one clone.
- The scoped checks (`UValueCheck`, `GlazingCheck`) never write to the model. After the PR3 fix they give the same
  warning text as Edit > ModelCheck for the affected objects.
- The 3D view can already colour by construction and by aperture construction (`ConstructionAppearanceSettings`).

## Verdict

**The engineering core of PR1–PR3 is right and should survive almost entirely.** The UX shape still follows the tools
it replaced:

- two windows named after *calculators*;
- three menus that lead to each of them;
- the most important decision, which elements change, inside a collapsed *Advanced* section;
- validation only *after* Apply.

From a clean start, the normal path never shows "calculator", "duplicate", "assign", "layer" or "ModelCheck".

The recommendation is **one Thermal Performance panel**. It handles any mix of selected envelope elements, grouped by
construction. Every change in it means **"choose a better version of this construction"**:

- **Opaque elements:** a generated variant, which adjusts the legitimate insulation layer, competes with existing
  constructions that meet the target.
- **Glazing:** only complete, real systems compete.

A change set gives one Apply and one Undo, and validation is shown *before* Apply.

The click saving over PR2b and PR3 is modest: 5–6 interactions down to 4. The larger gains are:

- fewer concepts and one place to look;
- inspection with no extra click;
- no modal window to close;
- walls and windows changed together;
- warnings before the change instead of after it.

## 1. User mental model

> **Every envelope element has a construction. The construction carries its performance and is shared by N elements.
> To improve performance, I choose a better version of that construction, either for every element that shares it or
> only for the elements I picked.**

| Stays visible (engineering concepts) | Leaves the normal path (implementation concepts) |
|---|---|
| Construction **name**: users know it, and Tas and Part L depend on it | Construction Manager, Duplicate, Assign |
| Performance: U for opaque; Uw, Ug, Uf, g and LT for glazing | Choosing a layer: automatic, with a per-row override |
| **Used by N**, and **which N** (highlighted in 3D) | "Modify in place" vs "new construction": the option becomes **Keep name** |
| Target and margin | Calculator windows. Library vs model vs loaded file: a *Source* column |
| Scope: **all N** or **only selected** | ModelCheck as a separate step: part of the proposal |

| User intent | Answer |
|---|---|
| "Show me what construction this element uses" | Select the element; the panel shows it, **no extra click** |
| "Make these walls U = 0.18" | Type 0.18 in the U cell of the wall group. If the walls use several constructions, each gets its own proposal, still one Apply |
| "Use a better window here" | Choose *Change…* on the window row: candidates sorted by Uw, filtered by the target |
| "What would I have to change to meet this target?" | Opaque: the generated variant states the thickness, plus any existing constructions that meet the target. Glazing: the filtered candidates. Later: targets for the whole envelope |
| "Apply this to all similar elements" | The scope defaults to "all N using it". "Select all N" highlights them first |

## 2. Opaque wall

1. **Click a wall in 3D.** The panel, docked beside the view and following the selection, shows
   `WALL · SIM_EXT_SLD · U 0.260 · 12 walls use it (1 selected) · Mineral wool 80 mm (adjustable)`.
   That is the whole inspection.
2. **Click the U cell and type `0.18`.** The live preview (existing evaluator, about 0.25 s) shows:
   - `U 0.260 → 0.180 ✓ · Mineral wool 80 → 124 mm · new name SIM_EXT_SLD U0.18`;
   - `or use one of 2 existing constructions that meet 0.18 ▸`;
   - the scope, inline and primary: `Changes (•) all 12 walls  ( ) only the 1 selected`;
   - the check before Apply: `✓ no new warnings` or `⚠ 12 new warnings ▸`.
3. **Apply.** The panel reads `12 walls now U 0.180. One Undo reverts it. [Details] [Report]`. There is no window to
   close.

That is **4 interactions**: click, click, type, Apply. PR2b needs 6; the legacy flow needs about 15–18.

Variants:

- **Several constructions selected.** For example, 40 walls on 3 constructions show a group header
  `WALLS · 3 constructions · 40 elements`. A target typed there creates 3 proposals and 3 preview rows, with one Apply
  and one Undo.
- **Target not reachable.** The panel says `Not reachable: best 0.025 at 1000 mm` and lists any existing constructions
  that do meet the target.
- **Rare overrides** sit behind `⋯` on each row: layer, thickness range, heat-flow basis and Keep name.

## 3. Window

1. **Click a window.** The panel shows
   `WINDOW · SIM_EXT_GLZ · Uw 1.35 (Ug 1.24 / Uf 2.20) · g 0.40 · LT 0.80 · 20 windows (1 selected)`.
2. **Choose *Change…* or type a target Uw.** A list of candidates opens under the row:
   - complete systems from the model and from remembered sources;
   - columns `Uw | Ug | Uf | g | LT | Frame | Source`, sorted by Uw and filtered by the target;
   - problems marked **on the candidate row, before it is chosen**:
     - `made for roofs`: the system's Default Panel Type does not match the host panels;
     - `no frame: windows lose their frame`;
     - `material differs from model`.
3. **Click a candidate.** The panel shows Current, Proposed and Margin, the scope inline, and the check before Apply.
4. **Apply.**

That is **4 interactions**; PR3 needs 5 plus Close.

Glazing rules that do not change:

- only complete systems are offered;
- Uw is computed from the real pane and frame areas, and the 80/20 estimate is labelled as such;
- pane properties are never edited and frames are never invented;
- a loaded source never touches the model before Apply.

Sources:

- *Add source…* is needed once per machine. Sources are remembered and reloaded through the existing
  `GlazingSourceCache`, in under 1 s when cached.
- A file the cache already knows to be pane-only, such as IGDB, says so before the 67 s first load.

## 4. Layout and architecture

```
┌ Thermal performance ────────────── [ Selection | Whole envelope ] ┐
│ 3 elements selected · 2 constructions              [Select all ▸]  │
│                                                                    │
│ WALLS                                            Target ≤ [0.18 ]  │
│  SIM_EXT_SLD   U 0.260 → 0.180 ✓   12 use it (2 sel.)          ⋯   │
│    Mineral wool 80 → 124 mm · or 2 existing meet target ▸          │
│    Changes (•) all 12   ( ) only 2 selected   [ ] keep name        │
│ WINDOWS                                                            │
│  SIM_EXT_GLZ   Uw 1.35  g 0.40  LT 0.80   20 use it (1 sel.)       │
│    [Change…]                                                       │
│ ────────────────────────────────────────────────────────────────── │
│ Before apply: ✓ no new warnings                         [Details]  │
│ 1 change · 12 walls          [Discard]  [Apply]  one Undo reverts  │
└────────────────────────────────────────────────────────────────────┘
```

**Levels**

- **Primary, in the panel:** inspect, target, scope, check before Apply, Apply.
- **One click away:** the candidate list, per-row overrides (`⋯`), Details (the existing `LogWindow`), the report,
  and highlighting the affected elements in 3D.
- **Expert tools, separate and unchanged:** the Construction and Material library editors (layer editing, naming,
  "create without assigning"), the full Edit > ModelCheck, and the Legacy calculators until they are retired.

**Whole-envelope mode** applies when nothing is selected. It lists every construction under Walls, Roofs, Floors,
Windows and Doors, with U or Uw/g/LT, element count, area and target margin. Clicking a row highlights its elements in
3D, and rows are editable in the same way. This replaces the PR4 "U-value / Used-by columns" idea and the batch tables
of the classic calculators.

**Architecture underneath** (reuse first)

- **`ThermalChangeSet`** is an ordered list of proposals (`AdjustLayer`, `ReplaceConstruction`, `ReplaceGlazing`), each
  with an explicit scope. Apply:
  1. runs the existing internal `AnalyticalModel.SetUValue(...)` and `SetGlazing(...)` on **one** clone;
  2. runs the post-steps once (`UpdateConstructions`, `UpdateApertureConstructions`,
     `Tas.Modify.UpdateThermalParameters`, aperture U/g/LT);
  3. calls `SetJSAMObject` exactly once.
- **Check before Apply:** run `UValueCheck` / `GlazingCheck` on the proposed clone and on the current model, and show
  only the *new* warnings. Neither check writes to the model, so this is safe.
- **Evaluators stay as they are:** `TasUValueEvaluator`, `TasGlazingEvaluator`, `StaSingleFlightWorker`, and the
  range-end cache.
- **Candidate provider:** extend `GlazingSource` / `GlazingSourceReader` / `GlazingSourceCache` to opaque
  constructions. The U of an opaque candidate comes from `ThermalTransmittanceCalculator.Calculate` (about 220 ms each),
  run as a batch and cached.
- **Inspection reads stored parameters** and makes no Tas call. A missing or out-of-date value shows "not calculated"
  and a *Recalculate* action.
- **The scope stays explicit while editing.** Once a proposal exists, its scope is **pinned**
  (`Editing 12 walls; changing the selection won't change this`). Any change to the model from elsewhere, such as an
  Undo or another editor, discards pending proposals.

## 5. PR1–PR3, part by part

| Part | Verdict | Reason and future form |
|---|---|---|
| PR1a type-based layer picker (SAM_Tas) | **KEEP** | Correctness; the generated variant depends on it |
| PR1b failure classification and messages | **KEEP** | Every preview uses them |
| Evaluator interface, single-flight STA worker, range-end cache, live preview | **KEEP** | The strongest part: about 0.25 s, and an unreachable target answered in 0 ms |
| `Modify.SetUValue` / `SetGlazing` (one clone, one Undo, unique naming, Guid identity, heat-flow basis) | **KEEP, SIMPLIFY** | Combine them through `ThermalChangeSet`; their model-level cores already allow it |
| `UValueViewModel` / `GlazingViewModel` | **KEEP** as row view-models, **SIMPLIFY** | Share scope, apply, check and report; merge `UValueApplyScope` and `GlazingApplyScope` |
| Separate opaque and glazing windows | **REPLACE** | One panel; the opaque and glazing parts survive as row content and the candidate list |
| Tools > U Value / Glazing Calculator split buttons | **REMOVE FROM PRIMARY UX** | One ribbon toggle, "Thermal performance", shows the panel; the classic tools sit in a **Legacy** drop-down |
| 3D right-click "Set U-value..." / "Set glazing..." | **SIMPLIFY** | One item, "Thermal performance…", for panels, apertures or both; the Assign items move to an "Assign (classic)" submenu, then go |
| Edit > Constructions "Set U-value…" button (close, then Yes/No/Cancel) | **REMOVE FROM PRIMARY UX** | Closing one editor to open another shows that two editors work on the same data. It stays only until the panel ships; the library remains an expert data tool |
| Advanced section | **SIMPLIFY** | **Scope** (all or selected) moves to the main area. "Modify in place" becomes **Keep name**. "Don't assign" leaves this flow later; it belongs to library editing. "Include library/loaded" becomes a visible Source filter. Layer, thickness range and heat flow stay as per-row overrides |
| Classic calculators | **REMOVE FROM PRIMARY UX** (Legacy) | Removed only after explicit parity testing: the batch U table (whole-envelope mode), assigning an existing construction by U (opaque alternatives), and g-value selection (glazing candidates) |
| Reports | **KEEP** for now | Becomes one append-only change log per model once the panel is stable; Copy stays |
| ModelCheck presentation (scoped line after Apply, plus Details) | **KEEP** the scoped check, **SIMPLIFY** the timing | Shown **before** Apply as new warnings; after Apply only the result line. The full ModelCheck stays in Edit |
| Construction libraries | **KEEP** as sources, **REMOVE FROM PRIMARY UX** as a concept | Users see a *Source* column, not "library" |
| "Load more…" | **SIMPLIFY** | *Add source…*, remembered between sessions through the existing cache; pane-only files flagged before loading |
| Live preview | **KEEP** | Extended to the check before Apply |
| Target-U workflow | **KEEP, ELEVATE** | The target becomes the main control for both kinds of element, at group level too; envelope targets later |
| Part O look (`PartOStyles.xaml`) | **KEEP** | The Part O step strip was rightly not used |
| PR3 style follow-ups | **KEEP only what survives** | The amber warning brush, and candidate-grid row height and truncation. Copy All placement in the legacy windows is not pursued |

## 6. What survives

- **All engine work:** the layer picker, classification, evaluators and worker, both `Set*` cores, naming, heat-flow
  basis, Guid candidate identity, the glazing reader and cache, `GlazingUw`, both scoped checks, the report text.
- **Both view-models,** as the logic of a row.
- **The invariants:**
  - one clone and one Undo per Apply;
  - Cancel leaves the model and its history untouched;
  - imported sources stay out of the model until Apply;
  - pane and frame data are never invented.
- **The tests** that guard these invariants (PR2a/2b/3).
- **The Part O look.**
- **The acceptance method:** UIA interaction counts, ModelCheck before and after, Undo/Redo in the real app.

## 7. What eventually disappears

- `SetUValueWindow` and `SetGlazingWindow` as separate top-level windows (their content moves into the panel).
- The two Tools split buttons.
- Three of the four construction items per element kind in the 3D context menu.
- The "Set U-value" / "Set glazing" buttons in the library lists, and the step that closes the list to open the
  window.
- "Don't assign" and "Modify in place" as user-facing terms.
- The Advanced section as the home of scope.
- The classic U-value and glazing calculators, after parity testing.
- The legacy "Assign Construction By UValue" and "By gValue".
- One report file per Apply, after the change log replaces it.

## 8. PR4

**Replaced.** As planned, PR4 adds U-value and Used-by columns to a modal legacy list and restyles legacy windows that
are due to be retired; both invest in what this review demotes. Only two pieces carry forward, and both go into
Stage A-lite:

- the warning before Apply when a system is incompatible with its host panels (the open owner item after PR3);
- the PR3 style fixes that will survive.

## 9. Staged migration

Each stage ships on its own, keeps the tests for one Undo per Apply, for Cancel leaving the model untouched and for the
scoped check matching ModelCheck, and is accepted with a UIA interaction count in the real app.

| Stage | Scope | Risk | Retires |
|---|---|---|---|
| **A-lite: only what survives** | (1) Warnings before Apply: candidate rows marked for panel-group mismatch, frameless and differing material; the scoped check run on the proposed clone, new warnings shown before Apply. (2) Scope moved out of Advanced in both windows. (3) One scope model shared by both view-models, with one scope-text builder. (4) "Keep name" replaces "Modify in place", disabled with a reason for "only selected" or a shared name. (5) Essential PR3 style fixes: amber warning brush, candidate-grid row height and truncation. No other polish of legacy windows | Low | Old PR4 |
| **B0: docking spike** | Time-boxed spike: can `AnalyticalWindow` host a docked side panel next to the view tabs (show, hide, resize, persist width) without disturbing the viewport, the tree or the ribbon? Outcome: docked panel, or the same control in a modeless tool window for now | Low | n/a |
| **B: read-only Thermal Performance panel** | Panel (or the B0 fallback) that follows the selection, plus whole-envelope mode: U, Uw/g/LT, count, area, source; clicking a row highlights in 3D. Reads stored parameters only and never writes the model. Ribbon toggle; the right-click item opens the panel; its edit buttons open the existing windows pre-filled | Low (read-only) | The PR4 columns idea |
| **C: editing in the panel** | `ThermalChangeSet` combining the existing `Set*` cores on one clone; the U cell uses `UValueViewModel`; *Change…* uses `GlazingViewModel` as a list under the row; mixed selections and several constructions at once; pinned scope; one Apply, one Undo. Existing reports kept | Medium (new composition and selection sync) | `SetUValueWindow`, `SetGlazingWindow`, the library hand-over buttons |
| **D: opaque alternatives and remembered sources** | Existing constructions that meet the target, shown beside the generated variant; *Add source…* remembered for opaque and glazing; pane-only files flagged | Medium (batch U cost, eased by the cache) | "Assign Construction By UValue / By gValue" |
| **E: retire legacy** | Explicit parity tests of the panel against the classic calculators, then removal; context-menu Assign items removed; the Tools split buttons become one entry; reports become one append-only change log per model | Low | Classic calculators, one report file per Apply |
| Later, separate scope | Envelope targets (e.g. Part L limiting values) with "propose fixes for everything failing"; IGDB pane + gap + pane composition | n/a | n/a |

Classic calculators stay reachable, under **Legacy**, through Stages B to D.

Acceptance targets:

- inspection: **1** interaction;
- opaque change: **4** interactions;
- glazing change: **4** interactions;
- walls and windows changed together: one Apply, one Undo.

## Risks

1. **Docking.** SAM has no docking infrastructure (Stage B0 decides). The fallback, a modeless tool window, uses the
   same control and view-model, so nothing after B is lost.
2. **Selection sync and changes from elsewhere.** An Undo from the ribbon while a proposal is pending must discard the
   proposal. Pinning the scope keeps "what will change" explicit.
3. **Out-of-date stored performance.** The whole-model `UpdateThermalParameters` ignores aperture constructions (PR3,
   deviation 2), so the panel shows where a value came from and offers "not calculated / Recalculate".
4. **Batch U for opaque alternatives** costs about 220 ms per construction and needs a cache keyed like
   `GlazingSourceCache`.
5. **"Keep name" with "only selected" is not possible.** `UpdateConstructions` matches by name, so two constructions
   cannot safely share one. The control is disabled and says why.

## Next step

Stage A-lite on `feature/thermal-stage-a-lite-<date>` from `sow/2026-Q3`, then the B0 docking spike.
