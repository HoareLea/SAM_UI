# Thermal Stage A-lite (1 Oct 2026)

Stage A-lite of the thermal performance redesign (design: `documentation/plans/Thermal-Performance-Review.md`, PR
[SAM-BIM/SAM_UI#165](https://github.com/SAM-BIM/SAM_UI/pull/165)). It contains only changes that survive into the future
Thermal Performance panel, applied to the two windows from PR2b ("Set U-value") and PR3 ("Set glazing"). The classic
calculators and every other legacy window are untouched. **No engine change:** the evaluators, `Modify.SetUValue`,
`Modify.SetGlazing`, the checks and the reports behave as before (the scope enum they carry was renamed).

## What changed

| # | Change | Where |
|---|---|---|
| 1 | **Warnings before Apply (glazing).** A candidate row is marked before it is chosen, in a new *Check* column, in amber: `made for roofs/floors/walls` (its Default Panel Type is of another panel group than the panels carrying the apertures in scope), `no frame` (the current system has one) and `material differs from model` / `material missing` (blocked, as before). Once chosen, the same facts are full sentences above Apply (a blocker stays the red block reason, not a warning). The panel-group marker uses the same panel-group rule as the scoped post-Apply check, and is recomputed when the scope changes | `GlazingViewModel.RowWarnings`, `GlazingRowWarning`, `GlazingWarningKind`, `GlazingCandidateRow.Warnings` |
| 2 | **Scope out of Advanced.** *Changes: (•) All N panels/apertures using it ( ) Only the M selected* is in the main scope box of both windows, with the labels carrying the counts. "Only the selected" is disabled when no selected element uses the construction (and while Keep name is on), with the reason beside it. *Don't assign* stays under Advanced as a check box | `SetUValueWindow`, `SetGlazingWindow` |
| 3 | **One scope model.** `UValueApplyScope` and `GlazingApplyScope` are replaced by `ThermalApplyScope` (`AllUsing`, `SelectedOnly`, `DontAssign`) and one `ThermalScope` class (the pinned element Guids, the choice, the labels, the availability and the one scope-sentence builder) used by both view-models. The scope is **pinned**: the using / selected Guid lists are copied when the view-model is created, so a selection that changes afterwards does not change what Apply touches | `ThermalScope`, `ThermalApplyScope`, both view-models, requests, results, reports |
| 4 | **Keep name** replaces "Modify in place" in every user-facing text (check box under Advanced, scope / result sentences, block text, report line "name kept, construction modified"). It is **disabled with the reason shown** when: another construction shares the name (the legacy post-step matches by name), the scope is "only the selected" (a name cannot be kept for some panels only), or Don't assign is chosen; turning Keep name on disables "only the selected" and Don't assign the same way. The enum value `UValueApplyMode.ModifyInPlace` keeps its name (internal) | `UValueViewModel.KeepName`, `…UnavailableReason` |
| 5 | **PR3 visual fixes that survive.** A `PartO.Brush.Warning` amber (#9A6700) is added and used for the warning glyph (both windows), the Check column and a warnings-only post-Apply check (Danger stays for errors). The candidate grid rows are 24 px (as the Part O Mixed Design grid), text cells end in an ellipsis instead of being cut, numeric cells are centred, the window is wider (1260 px) to fit the new column. Copy All placement and every other style difference recorded in PR3 are **not** pursued | `PartOStyles.xaml`, `SetGlazingWindow.xaml` |

## Deviations from the review

* The review's Stage A-lite item "the scoped check run on the proposed clone, new warnings shown before Apply" is **not
  implemented**: the owner's overnight scope listed only the three candidate warnings, and running both checks on a proposed
  clone belongs with the change set of Stage C. The panel-group marker is instead tested to **predict** the post-Apply check.
* "Don't assign" stays a (rare) Advanced check box rather than a third main-area choice; the review has it leaving this flow
  later.

## Tests

New: `ThermalScopeTests` (counts, labels, sentence builder, basis, pinned for both view-models, both view-models share the
model, the view-models never write the model), `KeepNameTests` (availability rules and reasons, Apply with Keep name keeps
name and Guid in one history step, no user-facing text says "in place" / "modify"), `GlazingRowWarningTests` (panel group,
scope-dependent counts, frameless, material, current row clean, several markers, markers predict the scoped check, no model
write), `ThermalStageALiteWindowTests` (scope is outside Advanced in both windows, Keep name / only-the-selected exclude each
other with the reason shown, Apply with only-the-selected is one Undo step, Cancel leaves the model and history untouched,
markers and amber warning in the real windows, 24 px rows with ellipsis). Existing tests were updated for the renamed enum and
wording only. **Full `SAM.Analytical.UI.WPF.Tests`: 1854/1854** (1814 before + 40 new). One earlier full run had a single
load-dependent failure in an unrelated Part O progress-window test (`The_progress_window_keeps_its_content_after_standing_aside_for_a_dialog`);
it passes alone (2 runs) and in the re-run of the full suite. `Undo()` completes asynchronously on the UI thread, so unit tests
assert one history step per Apply and the Undo itself is proved in the real app.

## Real-app acceptance

App: copy of `SAM_UI\build` (this branch, `SAM.Analytical.UI.WPF.dll` md5 `AA86380E…`), fresh copy of the PR3 model, real Tas,
driven with UI Automation; evidence is local (`C:\TasOut\uvalue\alite`), as for PR2b / PR3.

| Check | Result |
|---|---|
| **Opaque:** Tools > U Value Calculator, `SIM_EXT_SLD`, target 0.18 | Reached U 0.180 with mineral wool 80 → 123 mm in 244 ms. The preview shows **Changes: (•) All 12 panels using it ( ) Only the selected** in the main area with Advanced collapsed (screenshot `u2-preview`); "Only the selected" is disabled (nothing selected) |
| Opaque Apply | `SIM_EXT_SLD U0.18` on 12 panels; check "No errors or warnings for SIM_EXT_SLD U0.18 and its 12 panels." |
| Opaque **one Undo** | Undo enabled → one click → Undo disabled, Redo enabled; saved model: 12 panels back on `SIM_EXT_SLD`, 4 constructions (identical to the original) |
| **Glazing:** Tools > Glazing Calculator, `SIM_EXT_GLZ` | Table in 0.6 s: the library `SIM_EXT_GLZ_SKY` is marked **⚠ made for roofs** and the library `SIM_EXT_GLZ` of Ug 2.24 **⚠ made for floors** (amber), the others carry no marker (screenshot `g1-table`); rows 24 px, Pane / Frame end in "…" |
| Glazing, chosen | Amber line above Apply: "SIM_EXT_GLZ_SKY is made for roofs (Default Panel Type Roof), but 20 of the 20 apertures sit in walls: Edit > ModelCheck will warn about them."; scope radios in the main area; Apply stays enabled |
| Glazing **Cancel** | Undo still disabled, Redo unchanged; saved model identical to the original (20 apertures on `SIM_EXT_GLZ`, 1 aperture construction, 18 materials, same U/g/LT) |
| Glazing **Apply** | 20 apertures on `SIM_EXT_GLZ_SKY`, 2 aperture constructions, 19 materials; scoped check "40 warnings for SIM_EXT_GLZ_SKY and its 20 apertures." in an amber box - the warnings the marker announced (the same count as the PR3 full-ModelCheck comparison for this system) |
| Glazing **one Undo** | Undo disabled, Redo enabled; saved model identical to the original again |
| **3D route** (select by Guid, right-click, Set U-value...) | Window opens with "used by 12 panels (1 selected)"; **Changes: All 12 panels using it / Only the 1 selected**. With "Only the 1 selected" chosen, Keep name is **disabled** with "Keep name changes every panel using SIM_EXT_SLD, so it cannot be combined with only the selected panels." Switching back to All and ticking Keep name disables "Only the 1 selected" and Don't assign, each with its reason (screenshot `m4-keep-name`) |
| 3D route, **only the selected, Apply, Undo** | `SIM_EXT_SLD U0.18` on 1 panel, 11 stay on `SIM_EXT_SLD`; check clean; one Undo restores 12 panels on `SIM_EXT_SLD` |

Interactions (Tools route, opaque): open, pick the construction, type the target, Apply = 4 (+ Close). Unchanged from PR2b; the
gain is that the scope is visible without opening Advanced.

## Observations, not changed

* In the real app the construction picker items of "Set U-value" expose only their type name to UI Automation (no `ToString`,
  unlike the glazing picker). Not part of A-lite; worth one line when the panel replaces the window.
* The full-model scoped check before Apply (review item) and the remaining PR3 style differences stay open for later stages.
