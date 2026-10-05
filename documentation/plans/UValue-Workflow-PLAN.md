# U-value workflow: plan (gate-reviewed 2026-10-01)

Status: **approved for implementation**, with the gate-review changes folded in. Base branch for SAM_UI and SAM_Tas
PRs: `sow/2026-Q3`.

## Goal

Changing a U-value should feel like the Part O / TM59 workflows: from a selected wall panel to a changed U-value in
about **4-6 meaningful interactions** (today ~15-18 clicks across 6 windows). The journey below is a design
hypothesis; steps may be removed, merged or renamed after the real-app review.

UX acceptance criteria (outcomes):

1. About 4-6 meaningful interactions from a selected panel to an applied U-value.
2. The default path succeeds with no layer picking and no error recovery.
3. Before Apply the user always sees: the construction, U before/after, the affected elements, the physical change
   (layer/thickness for opaque; pane/frame/system for glazing) **and the apply scope inline**, e.g.
   "Applies to 40 panels using SIM_EXT_SLD (3 selected)".
4. Every automatic choice can be overridden, under "Advanced" only.
5. One Apply, one Undo. No confirmation windows that carry no decision.
6. A scoped validation and a change report run automatically after Apply. The user sees a short success/warning line;
   the log and the report are one click away.
7. Opaque and glazing share the visual language but not the method. Opaque adjusts one variable layer. Glazing
   selects complete, real glazing systems and never edits intrinsic pane properties.

## Current journey (measured)

Edit > Constructions > select row > Duplicate > OK > OK > Tools > U Value Calculator > pick construction > OK > type U >
OK > result OK > 3D view > select panel > right-click > Select > By Construction Name > right-click > Assign Construction >
search > pick > OK > Edit > ModelCheck > read log.

Real-app test on `C:\TasOut\uvalue\model.sam` (a copy): steps 1-2 OK. Step 3 (U=0.5 on `SIM_EXT_SLD 1`) **fails** with
"Could not calculate construction for given criteria."

### Root cause (verified against code and model)

`SAM_Tas Create/LayerThicknessCalculationData.cs` pre-selects the layer with the lowest conductivity among layers of
10 mm or more. The wall's layers are Air 50 mm / cement particleboard 12 mm / mineral wool 80 mm / Air 50 mm /
rainscreen 3 mm. The model's **gas materials have real, positive conductivities** (0.024 and 0.01622 W/mK), below the
mineral wool's 0.025, so the air cavity genuinely wins. NaN can never win (`NaN < min` is false), so the "0 or NaN"
hypothesis was wrong. The TCD-side fallback in `ThermalTransmittanceCalculator.cs` (`layerIndex == -1`) only skips
conductivity <= 0, has no 10 mm filter, and would also pick gas. **The fix is type-based exclusion** (gas and
transparent), with conductivity <= 0/NaN kept only as a secondary guard.

## Design language to reuse

- Look: `WPF\SAM.Analytical.UI.WPF\Themes\PartOStyles.xaml` (palette, `PartO.SectionHeading`, `PartO.Caption`,
  `PartO.PrimaryButton`, `PartO.StatusGlyph`, wrapping tooltips).
- State line (optional, read-only): `PartOWorkflowStep` / `PartOWorkflowStepStripControl`.
- Results: `Actual | Limit | Margin | Status` table, Copy All, and a saved plain-text report with a provenance block,
  as `SavePartOTM59Report` does.
- Long work: `documentation/ProgressDialogPattern.md`.

## Chosen pattern: B, one progressive window

Target on top, live preview table below, Apply at the bottom, Advanced collapsed. No Next/Back strip. The decision is
confirmed after a timing spike of the real calculation (see the execution model below).

Entry points open the same window: right-click selected panels > "Set U-value...", the Edit > Constructions toolbar,
and Tools > U Value Calculator (with the picker pre-opened).

### Opaque journey

1. **Open** from the selected panels. The construction is pre-filled and the window shows Current U and Panels using it.
   The target box is pre-filled with the current U and selected, so typing replaces it.
2. **Type the target U-value.** The adjustable layer is auto-detected, using the same SAM_Tas picker fixed in PR1
   (never gas or glass). A sentence states it: "Mineral wool 80 mm will be adjusted; other layers stay fixed."
3. **The preview updates**: calculated thickness, achieved U, before/after layer table, margin, status glyph and the
   inline apply scope. Problems are explained in words, e.g. "Not reachable within 1000 mm (best U = 0.61)".
4. **Apply.** By default this creates a new construction (name via `Query.UniqueName`, e.g. `SIM_EXT_SLD U0.50`) and
   assigns it to every panel using the old construction. Under Advanced: modify in place, selected panels only, don't
   assign, layer override, thickness range.
5. **After Apply, automatically**: a scoped check and a saved `U-VALUE CHANGE` report.

### Engineering rules (opaque)

- **Heat-flow basis** comes from the affected panels' `PanelType` and falls back to the construction's
  `Default Panel Type` (`Tas.Query.ThermalTransmittance(PanelType, out hfd, out external)`). When the panels span mixed
  panel groups (wall/roof/floor), show a warning line.
- **Identity**: the affected set is the panels whose construction Guid matches
  (`AdjacencyCluster.GetPanels(Construction)`, by `TypeGuid`). `Analytical.Query.UpdateConstructions` matches by
  **name**, so warn when other constructions share the name.
- **Reachability**: if the achieved U differs from the target beyond tolerance, evaluate U at the maximum thickness and
  report the best achievable U. Apply stays disabled.

### Execution model (live preview)

Each evaluation creates a temp `.tcd` file and drives the TCD COM bisection (`Tas.Modify.Run`, which swallows
exceptions).

- Put an evaluator interface behind the view-model: a fake in unit tests, the real `ThermalTransmittanceCalculator`
  in the app.
- Run evaluations on a single STA worker with debounce and stale-result cancellation (single-flight).
- If the timing spike shows more than about 1 s per evaluation, use an explicit "Calculate" button instead.
- Failure classification from existing result fields, verified in PR1:
  - initial U is NaN: the calculation is unavailable (Tas/TCD could not run);
  - initial U is valid but the thickness/achieved U is NaN or off target: the target is unreachable;
  - inputs checked before the call: heat-flow direction undefined, or no adjustable layer.

### Model mutation and Undo

Undo is one snapshot per `SetJSAMObject` (`SAM.Core.UI/Classes/UIJSAMObject.cs`), and `JSAMObject` returns a deep clone.

- Do all edits on one clone, then call `SetJSAMObject(model, new FullModification())` **exactly once**.
- Make no intermediate `uIAnalyticalModel.JSAMObject = ...` assignments; `EditApertureConstructions` and
  `EditMaterialLibrary` do this, and each would add an Undo step.
- Keep the legacy post-steps: `Analytical.Query.UpdateConstructions`, `UpdateApertureConstructions` and
  `Tas.Modify.UpdateThermalParameters`.
- The scoped check and the report never write to the model.

### Validation and report

`UI.Modify.Check` runs the whole-model `analyticalModel.Log()` in a modal `LogWindow`, and `LogRecord` carries no object
reference.

- The automatic check reuses the per-object `Create.Log(Construction | ApertureConstruction | Panel, MaterialLibrary)`
  in SAM `SAM.Analytical/Create/Log.cs`, run over the affected objects.
- "Details" opens the existing `LogWindow`.
- The report lists constructions, layer, old/new thickness, old/new U, scope and check summary. It is stamped
  "applied at <time>; may since have been undone" and saved next to the model's output folder, as
  `SavePartOTM59Report` does. If the model is unsaved, only Copy All is offered.

### Glazing journey

The same UX language, but selection instead of calculation.

- **Candidates are complete glazing systems**: `ApertureConstruction`s (pane stack with gas gaps, plus frame) from the
  model plus imported construction libraries. They are never single pane materials: a `TransparentMaterial` is one
  sheet and does not define Ug. No pane properties are created or edited.
- **"Load more glazing..."** reuses the json/.tcd reader behind `ApertureConstructionLibraryWindow.ConstructionManagerImporting`
  (`WPF/.../Modify/EditApertureConstructions.cs`), factored into a query. It does not use the Material Library import.
  Loaded candidates stay in a **window-local pool**. On Apply, only the chosen construction and its materials enter
  the model, in the same single Undo step. Cancel leaves the model and the Undo history untouched.
- **Comparison table** per candidate: Ug, Uf, g-value and light transmittance (from `CalculateGlazing`), plus overall
  Uw. Uw is computed from the affected apertures' actual pane/frame areas; otherwise it is labelled
  "approx. Uw (80/20)". The existing `ApertureConstructionCalculationData` path uses a fixed 0.8·Ug + 0.2·Uf and no
  g/LT filter, so that figure is never presented as the overall U.
- Before Apply: `Current | Proposed | Target | Margin | Status`, frame and pane changes, and the inline scope. Apply,
  Undo, check and report behave as for opaque.

## PR boundaries and order

| PR | Repo(s) | Scope | Depends on |
|---|---|---|---|
| **PR1a** | SAM_Tas | Type-based layer picker in `Create.LayerThicknessCalculationData` and the same rule in the `ThermalTransmittanceCalculator` fallback; controlled "no adjustable layer" result; tests | - |
| **PR1b** | SAM_UI | Specific error messages in the legacy calculator flow, using the failure classification; before/after real-app evidence | PR1a merged, SAM_Tas rebuilt |
| **PR2** | SAM_UI | New U-value window (view-model + evaluator seam + tests), `Modify.SetUValue` reusing `ThermalTransmittanceCalculator`, `UpdateConstructions` and the assign logic; three entry points; scoped check and report; legacy dialogs stay | PR1a |
| **PR3** | SAM_UI | Glazing selection (candidate pool, comparison table, "Load more glazing..." reusing the existing importer) | PR2 |
| **PR4** (optional) | SAM_UI | U-value / Used-by columns in the Constructions list; restyle the remaining legacy windows to `PartOStyles.xaml` | PR2 |

- Dropped from PR1: the Duplicate guard, because PR2 removes Duplicate from the journey.
- SAM_UI references SAM_Tas as a pre-built DLL (`..\..\..\SAM_Tas\build\SAM.Analytical.Tas.dll`). Rebuild in order
  (SAM, then SAM_Tas, then SAM_UI) and watch for stale sibling `build/` DLLs.
- Installed-app acceptance needs an installer that contains the new SAM_Tas DLL.

## Verification

- **PR1a**:
  - Unit tests: on this exact wall (Air 50 / cement 12 / mineral wool 80 / Air 50 / rainscreen 3 with the model's
    conductivities) the picker chooses mineral wool.
  - Gas and transparent layers are never chosen.
  - A construction with only gas or glass layers gives a controlled "no adjustable layer" result.
  - The fallback in the calculator follows the same rule.
- **PR1b**:
  - The full WPF test suite passes.
  - Real-app UIA on a fresh copy of the model with the new build: U=0.5 succeeds first time on the default selection.
  - Each classified failure shows its specific message.
- **PR2**:
  - View-model tests: target reached within tolerance, layer override, apply modes, scope text, single Undo,
    automatic check and report.
  - Real-app UIA from the 3D right-click, counting interactions against 4-6.
  - One Undo restores all N panels and leaves no unused construction.
  - An unreachable target shows the inline message and keeps Apply disabled.
  - Reuse `documentation/evidence/parto-final-acceptance-2026-09-30/scripts/uia.ps1.txt`.
- **PR3**:
  - Glazing acceptance in the real app.
  - "Load more" then Cancel leaves the model and the Undo history unchanged.
  - Check logs before and after.
  - Screenshots reviewed against the Part O windows.

## Critical files

- SAM_UI WPF (`WPF\SAM.Analytical.UI.WPF\`):
  - `Modify\ThermalTransmittanceCalculator_SingleConstruction.cs`, `Modify\AssignPanelConstruction.cs`;
  - `Modify\EditApertureConstructions.cs`, `Modify\AssignApertureApertureConstructionByThermalTransmittance.cs`;
  - `Controls\ConstructionCalculationDataControl.xaml(.cs)`, `Windows\AnalyticalWindow.xaml.cs`;
  - `Themes\PartOStyles.xaml`.
- SAM_UI core: `SAM_UI\SAM.Analytical.UI\Modify\Check.cs`, `SAM_UI\SAM.Core.UI\Classes\UIJSAMObject.cs`.
- SAM_Tas (`SAM_Tas\SAM.Analytical.Tas\`): `Create\LayerThicknessCalculationData.cs`,
  `Classes\ThermalTransmittanceCalculator.cs`, `Query\ThermalTransmittance.cs`, `Modify\Run.cs`.
- SAM: `SAM.Analytical\Create\Log.cs`, `SAM.Analytical\Classes\AdjacencyCluster.cs` (`GetPanels`),
  `SAM.Core\Query\MaterialType.cs`.
