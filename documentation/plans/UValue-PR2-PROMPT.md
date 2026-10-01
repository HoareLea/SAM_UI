Implement PR2 of the U-value workflow: the new one-window opaque U-value flow ("Set U-value..."), in two PRs of SAM_UI:
PR2a (engine: view-model, evaluator seam, `Modify.SetUValue`, tests, no window) then PR2b (window, entry points, scoped check,
report, real-app acceptance). Opaque constructions only: glazing is PR3.

## Read first
1. `SAM-BIM\AGENTS.md` and `SAM_UI\AGENTS.md` (PR record doc + PR description during the PR; `PROJECT_PROGRESS.md` only after
   merge, as a docs-only closeout commit on the base branch; never on a PR branch).
2. `SAM_UI\documentation\plans\UValue-Workflow-PLAN.md`: the approved plan. Follow "Chosen pattern: B", "Opaque journey",
   "Engineering rules (opaque)", "Execution model", "Model mutation and Undo", "Validation and report", "PR boundaries" and the
   PR2 verification list. UX acceptance criteria 1-6 are the definition of done.
3. `SAM_UI\documentation\UValue-LayerPicker-PR1.md` and the PR1 closeout in `SAM_UI\PROJECT_PROGRESS.md`
   (SAM_UI#160 + SAM_Tas#78, both merged): what is already fixed and what was found.
4. Memory notes on SAM_UI build traps, driving SAM Analytical.exe via UIA, and the installed-product smoke / MSIX trap.
   The PR1 driver scripts are reusable: `documentation/evidence/uvalue-workflow-2026-10-01/scripts/`
   (`ulib`, `launch`, `ribbon`, `act`, `pick`, `timed`, `vals`, `mouse`, `clickel`, `logrows`, `modelsum.py`) and the headless
   COM probe in `.../probe/`.

The base branch for both repos is `sow/2026-Q3`; fetch it first. Branches: `feature/uvalue-pr2a-engine-2026-10-0X` and
`feature/uvalue-pr2b-window-2026-10-0X` (use today's date). PR2b branches off PR2a after it merges.

## Facts already verified (do not re-derive)
- SAM_Tas#78: `Tas.Query.AdjustableLayerIndex(Construction, MaterialLibrary)` is the default adjustable layer (never gas or glass,
  >= 10 mm, conductivity > 0, -1 if none). SAM_UI#160: `Query.UValueCalculationMessage(...)` (two overloads, in
  `WPF/SAM.Analytical.UI.WPF/Query/UValueCalculationMessage.cs`) classifies failures; tolerance 0.01 W/m2K because Tas reports
  0.051 for a target of 0.05. Reuse both; do not duplicate the rules.
- Real calculator timing (headless probe, this VM): a reachable target takes about 0.25 s per evaluation, an unreachable one about
  1.5 s, an undefined heat-flow direction about 0.6 s. Through the legacy UI a whole calculation is about 1.7 s. Re-measure with the
  spike below before choosing live preview vs a Calculate button.
- Heat-flow `Undefined` gives initial U = NaN (looks like "Tas unavailable"), so the direction must be resolved before the call.
- Legacy apply (`Modify/ThermalTransmittanceCalculator_SingleConstruction.cs`) does `constructionManager.Update(result)`,
  `Analytical.Query.UpdateConstructions`, `UpdateApertureConstructions`, `Tas.Modify.UpdateThermalParameters`, then
  `uIAnalyticalModel.SetJSAMObject(model, new FullModification())`. `UpdateConstructions` matches by **name**.
- Legacy apply leaves the adjusted layer material (for example `I01_Mineral Wool_20kg/m3_0.025W/mK_0.034m`) **out of the model's
  Material Library**; Edit > ModelCheck then reports an Error. `Modify.SetUValue` must add it (a copy of the source material with the
  new default thickness, named as the legacy flow names it) in the same single Undo step.
- Panel membership: `AdjacencyCluster.GetPanels(Construction)`. Heat-flow basis: `Tas.Query.ThermalTransmittance(PanelType, out hfd,
  out external)` from the affected panels' `PanelType`, falling back to the construction's `Default Panel Type`. Note a construction's
  `Default Panel Type` can be the generic `Wall`.
- Undo is one snapshot per `SetJSAMObject` (`SAM.Core.UI/Classes/UIJSAMObject.cs`); `JSAMObject` returns a deep clone. Do every edit on
  one clone and call `SetJSAMObject(model, new FullModification())` exactly once.
- Existing context menu (3D view, `Windows/AnalyticalWindow.xaml.cs` ~4150): "Assign Construction", "Assign Construction By UValue"
  (`Modify/AssignApertureApertureConstructionByThermalTransmittance` family is for apertures; check what the panel one does and keep
  it). Select > By Construction Name works (selects every panel using that construction).
- `SearchWindow` (Select Construction) had a dead OK button; a separate session was started to fix it. Check
  `git log origin/sow/2026-Q3 -- WPF/SAM.Core.UI.WPF/Windows/SearchWindow*` before relying on it. The new window must not depend on it.
- Design language: `Themes/PartOStyles.xaml`, `PartOWorkflowStepStripControl`, `Actual | Limit | Margin | Status` table + Copy All, and
  `Modify.SavePartOTM59Report` (provenance block, plain text, "stamped, may since have been undone") as the report model.
  Long work: `documentation/ProgressDialogPattern.md`.
- Per-object validation: SAM `SAM.Analytical/Create/Log.cs` has `Create.Log(Construction | ApertureConstruction | Panel, MaterialLibrary)`.
  `UI.Modify.Check` runs the whole-model `Log()` in a modal `LogWindow` and `LogRecord` carries no object reference.

## PR2a: engine (no window)
Spike first (report numbers in the PR record, change the plan only if they contradict it): time 50 consecutive real evaluations with the
existing `ThermalTransmittanceCalculator` on the model copy (`C:\TasOut\uvalue\model.sam`; never touch the user's original) to decide
live preview (<= ~1 s) vs explicit Calculate; measure STA single-flight behaviour with cancellation of stale requests.

- **Evaluator seam**: interface (for example `IUValueEvaluator`) that takes construction + target + heat-flow basis + layer + range and
  returns the calculated thickness / achieved U / classified failure; real implementation wraps `ThermalTransmittanceCalculator` on a
  single STA worker with debounce and stale-result cancellation; a fake for tests.
- **View-model** (`INotifyPropertyChanged`, no WPF types, so it is unit-testable): source construction, current U, panels using it
  (`GetPanels(Construction)`), selected panels, target U (pre-filled with current U), auto-detected layer + the sentence "<layer> <t> mm
  will be adjusted; other layers stay fixed.", thickness range, preview rows (layer table before/after, achieved U, margin, status),
  inline apply scope text ("Applies to N panels using <name> (M selected)"), warnings (mixed panel groups; other constructions sharing
  the name; unreachable target with best achievable U evaluated at the maximum thickness), and an `ApplyEnabled` that is false until a
  reachable result is shown. Advanced: layer override, thickness range, apply mode (new construction [default] / modify in place),
  scope (all panels using it / selected only), don't assign.
- **`Modify.SetUValue`** (WPF `Modify`): on one clone of the model: new construction named with `Query.UniqueName` (for example
  `SIM_EXT_SLD U0.50`) or in place, adjusted layer thickness, adjusted material added to the Material Library, panels re-assigned per
  scope, legacy post-steps kept, **exactly one** `SetJSAMObject`. No intermediate `uIAnalyticalModel.JSAMObject = ...` assignments.
  Returns a result object (old/new construction, layer, old/new thickness, old/new U, panel count, scope) for the report.
- **Tests (xunit, `SAM.Analytical.UI.WPF.Tests`)**: target reached within tolerance; layer override; each apply mode and scope; scope text;
  unreachable target keeps Apply disabled and names the best U; heat-flow basis from panels vs construction fallback and the mixed-group
  warning; name-sharing warning; stale-result cancellation (fake evaluator with controllable completion); single Undo step
  (`SetJSAMObject` called once, one snapshot added); the adjusted material is in the Material Library afterwards and ModelCheck
  (`Create.Log`) reports no missing-material error for the new construction; unused source construction is left alone (no silent deletes).
- PR record `documentation/UValue-SetUValue-PR2A.md`; open the PR against `sow/2026-Q3`; wait for green CI; **merge only with my explicit
  approval**, then the `PROJECT_PROGRESS.md` closeout on the base branch.

## PR2b: window, entry points, check, report, acceptance
- **Window**: one progressive window per plan pattern B (target on top, live preview table, Apply at the bottom, Advanced collapsed,
  no Next/Back), styled with `PartOStyles.xaml` (headings, captions, primary button, status glyph, wrapping tooltips). Keyboard: Enter
  applies when enabled, Esc cancels, target box selected on open. Long evaluations use the progress-dialog pattern or an inline busy
  state. Legacy windows stay; nothing is removed.
- **Entry points** (all open the same window): right-click selected panels > "Set U-value...", the Edit > Constructions toolbar, and
  Tools > U Value Calculator (picker pre-opened; keep the legacy flow reachable until PR4 decides, for example as "U Value Calculator
  (classic)" only if I agree; otherwise leave the old ribbon button on the old flow and add a new button).
- **After Apply, automatically**: scoped check (`Create.Log` over the affected constructions and panels, never the whole model) with a
  one-line success/warning in the window and "Details" opening the existing `LogWindow`; a saved plain-text `U-VALUE CHANGE` report next
  to the model's output folder with provenance and the "applied at <time>; may since have been undone" stamp (constructions, layer,
  old/new thickness, old/new U, scope, check summary); Copy All; if the model is unsaved only Copy All. Neither writes to the model.
- **Tests**: window state tests where the Part O windows have them (headless render to confirm layout), report content and file naming,
  check summary mapping.
- **Real-app acceptance** (evidence in `documentation/evidence/uvalue-workflow-2026-10-0X/`, reuse the PR1 scripts; fresh model copy):
  - from the 3D right-click on `SIM_EXT_SLD` panels to an applied U-value, counting meaningful interactions against 4-6 (record the count
    and screenshots; compare with the measured legacy journey of ~15-18);
  - one Undo restores all N panels and leaves no unused construction and no orphan material; Redo reapplies;
  - an unreachable target shows the inline message with the best achievable U and Apply stays disabled;
  - Edit > ModelCheck after Apply reports **no** missing-material error (the PR1 finding);
  - modify-in-place, selected-panels-only and don't-assign each behave as stated; the saved model's panel constructions confirm it
    (`modelsum.py`);
  - the report file and the scoped check line; timings per evaluation in the window.
- PR record `documentation/UValue-Window-PR2B.md`; same merge discipline as PR2a.

## Out of scope
Glazing and aperture constructions (PR3), U-value / Used-by columns in the Constructions list and restyling of legacy windows (PR4), a
Duplicate guard, fixing `SearchWindow` (separate session), changing `Tas.Modify.Run`, changes in SAM core.

## Stop and ask me if
- the spike contradicts the plan (for example evaluations are too slow even for a Calculate button, or STA single-flight is unsafe);
- the one-Undo guarantee cannot be met without changing `UIJSAMObject` / SAM core;
- the Material Library handling needs a change outside SAM_UI (SAM / SAM_Tas);
- the window needs to change what the legacy Tools button does;
- an acceptance step contradicts the plan's assumptions (interaction count above 6, different Undo behaviour, a Tas failure not covered by
  the PR1 classification).
