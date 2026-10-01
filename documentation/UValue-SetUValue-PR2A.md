# U-value workflow PR2a: "Set U-value" engine, no window (PR record)

Branch `feature/uvalue-pr2a-engine-2026-10-01`, from `sow/2026-Q3` `ff68f3f9`. **Not merged.**
`PROJECT_PROGRESS.md` is not touched on this branch (closeout after merge, per `AGENTS.md`).
Plan: [plans/UValue-Workflow-PLAN.md](plans/UValue-Workflow-PLAN.md); brief: [plans/UValue-PR2-PROMPT.md](plans/UValue-PR2-PROMPT.md)
(committed here as instructed); PR1: [UValue-LayerPicker-PR1.md](UValue-LayerPicker-PR1.md) (SAM_UI#160 + SAM_Tas#78, merged).

## Status

Engine implemented, unit-tested (full WPF suite **1680 passed, 0 failed**) and exercised headless against the
real Tas calculator on a copy of the model. No window, no entry points, no UI change: the legacy calculator and
every existing window are untouched. PR2b (window, entry points, scoped check, report, real-app acceptance)
branches off this after it merges.

## Timing spike (before any code; `evidence/uvalue-pr2a-2026-10-01/spike/`)

Real `ThermalTransmittanceCalculator`, `SIM_EXT_SLD` on `C:\TasOut\uvalue\spike\model.sam` (a copy), one STA thread:

| Measurement | Result |
|---|---|
| 50 consecutive reachable evaluations (targets 0.15-0.39) | min 215, **median 246**, p90 278, max 313 ms |
| Unreachable target (U=5 and U=0.02, 6 runs) | **~2.7 s** each (the bisection runs to exhaustion) |
| U at a fixed thickness, one TCD run (`Calculate(Guid)`) | ~220 ms (1 mm: 1.4507, 80 mm: 0.2598, 1000 mm: 0.0246) |
| Single-flight worker, debounce 300 ms, typing "0.3"/"0.35"/"0.355" at 150 ms | 1 evaluation; result 537 ms after the last key |
| New keys while an evaluation is in flight | in-flight result discarded, only the last target delivered |
| Two evaluations on two STA threads at once | both correct (~0.4 s); single-flight kept anyway, per plan |
| Temp `.tcd` files left behind | 0 |

**Decision: live preview (no Calculate button).** A reachable evaluation is ~0.25 s, well under the plan's ~1 s.
**Refinement (not a contradiction):** an unreachable target would cost ~2.7 s, so the evaluator first computes U at
the minimum and maximum thickness (one TCD run, cached per construction/layer/range) - U falls monotonically
with thickness - and answers any target outside that band at once, with the best achievable U, without the
bisection. This is the plan's own reachability rule ("evaluate U at the maximum thickness"), done before instead
of after the bisection. Real-Tas probe: unreachable targets now settle in **0 ms** of evaluation.

## What is added

All in `WPF/SAM.Analytical.UI.WPF/` (namespace `SAM.Analytical.UI.WPF`):

- **Evaluator seam** `Interfaces/IUValueEvaluator.cs`: `EvaluateAsync(UValueEvaluationRequest, CancellationToken)`
  -> `UValueEvaluation` (thickness, achieved U, initial U, U at both range ends, best achievable U, PR1
  classification + message, elapsed ms). A cancelled token cancels the task even if the COM call cannot stop.
- `Classes/UValue/StaSingleFlightWorker.cs`: one dedicated STA thread, at most one pending item (a newer submit
  cancels it), debounce, stale in-flight results dropped, exceptions fault the task, `Dispose` cancels pending.
- `Classes/UValue/TasUValueEvaluator.cs`: the real evaluator (`ThermalTransmittanceCalculator` on the worker,
  300 ms debounce). Pre-checks with PR1 `Query.UValueCalculationFailure`; range ends first (cached); bisection only
  inside the band; a range end within the PR1 tolerance (0.01) counts as reached. The two TCD calls are injectable
  (internal ctor) so the logic is unit-tested without Tas.
- **PR1 classifier reused, not duplicated**: `Query/UValueCalculationMessage.cs` now has the enum-returning core
  `Query.UValueCalculationFailure(...)` (new `Enums/UValueCalculationFailure.cs`); both message overloads call it.
  The 13 PR1 tests are unchanged and pass.
- **View-model** `Classes/UValue/UValueViewModel.cs` (`INotifyPropertyChanged`, no WPF types): source construction,
  current U, panels using it (`AdjacencyCluster.GetPanels(Construction)`, by Guid), selected panels (only those
  using it), `TargetText` pre-filled with the current U (a "no change" target keeps Apply off), auto-detected
  layer (`Tas.Query.AdjustableLayerIndex`) and the sentence "<layer> <t> mm will be adjusted; other layers stay
  fixed.", reachable range, preview rows (before/after mm, rounded as applied), achieved U, margin, status, inline
  scope ("Applies to 12 panels using SIM_EXT_SLD (3 selected)."), result text ("Creates SIM_EXT_SLD U0.30;
  SIM_EXT_SLD stays unchanged."), warnings (mixed panel groups; other constructions sharing the name),
  `ApplyEnabled` only while a reached result for the current inputs is shown, `CreateRequest()`.
  Advanced: layer override, thickness range, heat-flow direction override, apply mode, scope (all / selected /
  don't assign). Every input change restarts the evaluation and cancels the previous one; stale results are dropped.
- `Query/UValueHeatFlowBasis.cs` + `Classes/UValue/UValueHeatFlowBasis.cs`: basis from the affected panels'
  `PanelType` via `Tas.Query.ThermalTransmittance(PanelType, ...)`, majority wins (ties to the construction's
  Default Panel Type), fallback to the Default Panel Type; `Mixed` drives the warning. Re-evaluated when the scope
  changes the affected panels (e.g. only the selected roofs).
- `Query/UValueConstructionName.cs`: "<source> U0.30", " (2)"... when taken (case-insensitive); an earlier
  " U0.50" suffix is replaced, not stacked. (There is no construction `Query.UniqueName` in SAM; this follows the
  existing Duplicate naming.)
- **`Modify/SetUValue.cs`**: `uIAnalyticalModel.SetUValue(request)` -> `SetUValueResult`. One clone
  (`JSAMObject`), all edits on it, **exactly one** `SetJSAMObject(model, new FullModification())`, none on failure.
  Adjusted material = copy of the source material named as the legacy flow names it (`<material>_<t>m`, 1 mm
  rounding) with the new default thickness, **added to the Material Library** (reused if it already exists).
  New construction (default, unique name) or in place; panels re-assigned with `Create.Panel(panel, construction)`
  per scope; legacy post-steps kept (`UpdateConstructions`, `UpdateApertureConstructions` with a manager holding
  only the changed construction, then `Tas.Modify.UpdateThermalParameters`, injectable for tests).
  Result: source/new construction, layer, old/new material, old/new thickness, old/new/target U, heat-flow
  direction, panel Guids, mode, effective scope, applied-at time (for the PR2b report).

## Decisions and assumptions

- **Modify in place is refused when another construction shares the name** (view-model `ApplyBlockReason`, and
  `SetUValue` returns an error): `UpdateConstructions` matches by name and would rewrite the namesake too. The
  default (new construction, unique name) is unaffected; the shared name is a warning line.
- In-place always applies to every panel using the construction (scope is ignored, shown in the scope text).
- "Don't assign" is a third scope value, not a separate flag (mutually exclusive with the other two).
- No silent deletes: `GetConstructions` lists panel-carried and stored constructions without merging, so a
  construction is stored as an object only when no panel carries it (the unused source, a "don't assign"
  construction) or when it was stored already (in place). This avoids a second copy of a construction.
- Heat-flow direction override added under Advanced (plan criterion 4: every automatic choice can be overridden);
  the PR1 message for an undefined direction is reused.
- Material name uses the invariant culture (`_0.067m`), the legacy code used the current culture (same text on
  this machine; avoids a decimal comma in a name).
- Applied thickness is rounded to 1 mm like the legacy apply (67.1 -> 67 mm); the reported new U is the evaluated
  one (0.300 at 67.1 mm). The difference is far inside the 0.01 tolerance.
- Nothing depends on `SearchWindow`.

## Real-Tas engine probe (`evidence/uvalue-pr2a-2026-10-01/engine/`)

Public API only (view-model + `TasUValueEvaluator` + `Modify.SetUValue` with the real thermal-parameter step,
Undo/Redo through a WPF dispatcher), fresh copy `C:\TasOut\uvalue\pr2a\model.sam`, `SIM_EXT_SLD` (12
`WallExternal` panels, 3 selected):

| Step | Result |
|---|---|
| Open | current U 0.2598, target pre-filled "0.26" -> NoChange, Apply off; range U 1.4507 (1 mm) .. 0.0246 (1000 mm); layer "I01_Mineral Wool_20kg/m3_0.025W/mK 80 mm will be adjusted..."; scope "Applies to 12 panels using SIM_EXT_SLD (3 selected)."; basis WallExternal / horizontal from panels |
| Type "0" "0." "0.3" at 80 ms | Reached: 80 -> 67 mm (67.1), achieved 0.3, 273 ms evaluation |
| 0.01 / 5 | Unreachable, 0 ms evaluation, "Best achievable: U 0.025 W/m²K at 1000 mm." / "U 1.451 W/m²K at 1 mm." |
| 0.05 / 0.5 / 0.3 | Reached (0.051 at 469.3 mm, within tolerance) / 33.8 mm / 67.1 mm, ~230-245 ms |
| Apply (451 ms incl. Tas `UpdateThermalParameters`) | 1 `Modified`, 1 `HistoryChanged`, `CanUndo`; `SIM_EXT_SLD U0.30` on 12 panels; source kept, unused; `I01_..._0.067m` **in the Material Library** |
| ModelCheck (`AnalyticalModel.Log()`) | before 0 errors, **after 0 errors** (legacy apply: 1 missing-material Error, PR1) |
| Undo | all 12 panels back on `SIM_EXT_SLD`; new construction and the added material gone (18 materials = original) |
| Redo | 12 panels on the new construction, material present |

Saved model (`modelsum.txt`): 12 `WallExternal` panels on `SIM_EXT_SLD U0.30` (wool 0.067), `SIM_EXT_SLD` unchanged.
Found by the probe and fixed: the reachable range was lost after the "no change" pre-fill (regression test added).

## Tests (`SAM.Analytical.UI.WPF.Tests`, 54 new)

Fixture `Helpers/UValueFixture.cs`: the real wall's layer order and gas conductivities on panels, plus a stand-in
for the two TCD calls whose physics reproduces the spike values (U = 1/(0.65 + t/0.025): 0.26 at 80 mm, 1.45 at
1 mm, 0.0246 at 1000 mm).

- `UValueEvaluatorTests` (15): worker on STA; typing burst runs only the last request; cancelling a running item
  completes it as cancelled and drops its result; exceptions; dispose. Evaluator: reachable via bisection on STA;
  range ends cached; below/above the band unreachable without bisection, with the best U and PR1 wording; range end
  within tolerance is reached; undefined heat flow, no adjustable layer, Tas unavailable use the PR1 messages;
  probe-only; layer override.
- `UValueViewModelTests` (20): pre-fill + Apply off; auto layer never the air gap + sentence; reachable within
  tolerance with rows and margin; unreachable keeps Apply off and names the best U; invalid text; layer override;
  thickness range; scope text for every mode/scope; selected-only with none selected blocks Apply;
  `CreateRequest`; heat-flow basis from panels, fallback to Default Panel Type, mixed-group warning, re-basis on
  selected roofs, undefined basis until a direction is chosen; name-sharing warning and in-place block; **stale
  result dropped** (manual evaluator, late completion); input change disables Apply; range kept after pre-fill.
- `SetUValueTests` (14 + 5 naming cases): new construction for all panels with the source kept unchanged; adjusted
  material in the library and ModelCheck (`Create.Log`) without a missing-material error, plus the guard that the
  same construction against the old library **does** report it; material reused; selected-only; selected-only
  with none refused; don't assign; in place; in place with a shared name refused; new construction leaves a
  namesake's panels alone; **one `SetJSAMObject`, one snapshot** (`Modified` and `HistoryChanged` once, `CanUndo`);
  thermal step runs on the committed model; failure leaves model and history untouched; result fields; naming.
- Full `SAM.Analytical.UI.WPF.Tests` (Release): **1680 passed, 0 failed**.
- Build order SAM (`dfec2a4c`, incl. SAM#175) -> SAM_Tas (`1761624f`, VS MSBuild) -> SAM_UI.sln (VS MSBuild,
  Release); `SAM_UI\build\SAM.Analytical.Tas.dll` and `SAM.Analytical.dll` md5 equal the sibling `build/` copies.

## Files changed

- New: `Classes/UValue/{StaSingleFlightWorker, TasUValueEvaluator, UValueEvaluation, UValueEvaluationRequest,
  UValueHeatFlowBasis, UValueLayerRow, UValueViewModel, SetUValueRequest, SetUValueResult}.cs`,
  `Enums/{UValueApplyMode, UValueApplyScope, UValueCalculationFailure, UValuePreviewStatus}.cs`,
  `Interfaces/IUValueEvaluator.cs`, `Modify/SetUValue.cs`, `Query/{UValueConstructionName, UValueHeatFlowBasis}.cs`.
- Changed: `Query/UValueCalculationMessage.cs` (enum core; same messages).
- Tests: `Helpers/UValueFixture.cs`, `UValueEvaluatorTests.cs`, `UValueViewModelTests.cs`, `SetUValueTests.cs`;
  `SAM.Analytical.UI.WPF.Tests.csproj` (+ `SAM.Architectural` reference: `ConstructionLayer` derives from its
  `MaterialLayer`).
- Docs: this record, `plans/UValue-PR2-PROMPT.md`, `evidence/uvalue-pr2a-2026-10-01/` (spike + engine probe).

## Open decision for PR2b (needs the owner)

Tools > U Value Calculator: **(a)** leave the ribbon button on the legacy flow and add a new "Set U-value" button, or
**(b)** point it at the new window (picker pre-opened) and keep the legacy flow reachable as
"U Value Calculator (classic)". The brief requires asking; PR2b will not change the legacy button until answered.

## Risks

- Mixed wall/roof panels on one construction get one U (majority basis); the warning says so, and choosing the
  selected panels re-bases. A per-group split is not attempted.
- The 0.01 tolerance accepts 0.051 for 0.05 (as PR1).
- Evaluation timings are from this VM; a slower machine still has the debounce and single-flight.

## Next step

Wait for green CI and explicit merge approval; then the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q3`.
Then PR2b on `feature/uvalue-pr2b-window-<date>` from the merged base, after the Tools-button decision above.
