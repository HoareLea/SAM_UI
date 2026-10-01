# U-value workflow PR2b: "Set U-value" window, entry points, check and report (PR record)

Branch `feature/uvalue-pr2b-window-2026-10-01`, from `sow/2026-Q3` `a023d9de` (PR2a merged as SAM_UI#162 `9ef5bd5`
+ closeout). **Not merged.** `PROJECT_PROGRESS.md` is not touched on this branch (closeout after merge).
Plan: [plans/UValue-Workflow-PLAN.md](plans/UValue-Workflow-PLAN.md); brief: [plans/UValue-PR2-PROMPT.md](plans/UValue-PR2-PROMPT.md);
engine: [UValue-SetUValue-PR2A.md](UValue-SetUValue-PR2A.md).

## Status

Implemented, unit/window-tested (full WPF suite **1700 passed, 0 failed**) and accepted in the real app (UIA-driven
`SAM Analytical.exe` from a copy of `SAM_UI\build`, real Tas, fresh model copy). Owner decision applied: **Tools > U Value
Calculator opens the new window; the legacy flow stays as "U Value Calculator (classic)"**.

## What is added

- **`Windows/SetUValueWindow.xaml(.cs)`**: one progressive window (plan pattern B), `PartOStyles.xaml` look (section
  headings, captions, primary Apply, status glyphs, wrapping tooltips). Top to bottom: construction picker (with panel
  counts) and facts line (current U, panels, selection, heat-flow basis); target box (pre-filled with the current U and
  selected, so typing replaces it) with the reachable range and the layer sentence; live preview (status glyph + words,
  `Actual U | Target U | Margin | Status`, before/after layer table, evaluation time); the **inline apply scope** and
  result line with warnings; **Advanced** collapsed (layer, thickness range, heat flow, new/in place, all/selected/don't
  assign); after Apply the scoped check line with **Details** (existing `LogWindow`), the report line, **Copy All**.
  Enter applies when enabled (Apply is the default button, disabled otherwise), Esc cancels; after Apply, Close becomes
  the default. No Next/Back; legacy windows untouched.
- **Entry points** (all open the same window):
  - 3D right-click on panels > **"Set U-value..."** (`Modify.OpenSetUValueWindow(panels)`: the construction most of the
    selection uses; the selection is kept for "Selected panels only"; selected panels on other constructions are noted).
  - **Edit > Constructions** toolbar: new opt-in **"Set U-value..."** button on `ConstructionLibraryWindow`
    (`SetUValueRequested` event, button visible only when a handler is attached, enabled for exactly one row). The list
    edits a copy, so it **closes first** (an OK there after a U-value change would overwrite it); with unsaved edits it
    asks Yes (save, one Undo step) / No (discard) / Cancel (stay).
  - **Tools > U Value Calculator** is now a split button: the button opens the new window with the **picker open**; its
    drop-down keeps **"U Value Calculator (classic)"** (the PR1 legacy flow, unchanged).
- **Scoped check** `Query.UValueCheckSummary`: SAM's per-object rules (`Create.Log(Construction | Panel,
  MaterialLibrary)`) over the changed construction and the panels it was assigned to - never the whole model - distinct
  by text, one line ("No errors or warnings for SIM_EXT_SLD U0.30 and its 12 panels."), glyph ✓ / ⚠ / ✕.
- **Report** `Query.UValueChangeReportText` + `Modify.SaveUValueChangeReport`: plain-text `U-VALUE CHANGE` with
  provenance (model, "applied at <time>; may since have been undone", method), construction(s), layer, old/new material,
  old/new thickness, old/new/target U and heat flow, scope, material added, and the check with its records. Saved in the
  model's folder (SAM's default simulation output folder) as `<model>_UValueChange_<yyyyMMdd-HHmmss>.txt`, never
  overwriting (" (2)"); an unsaved model gets Copy All only. Neither the check nor the report writes to the model.

## Real-app acceptance (`evidence/uvalue-pr2b-2026-10-01/final/`)

App: copy of `SAM_UI\build` at `bcdf24f0` (`SAM.Analytical.UI.WPF.dll` md5 `286b0e0f...`), `C:\TasOut\uvalue\app-pr2b`;
model: fresh copy `C:\TasOut\uvalue\pr2b\b\model.sam`. `drive.log` + `drive-inline.log` have every step with
timestamps; `shots/` PNG + UIA dump per window; `reports/` the saved reports.

| Check | Result |
|---|---|
| **Interactions**, 3D right-click to applied U | (1) click a `SIM_EXT_SLD` wall panel, (2) right-click, (3) "Set U-value...", (4) type `0.3`, (5) Enter = Apply, (6) Enter = Close: **6 including selecting the panel, 5 from a selected panel** - within 4-6; the measured legacy journey is ~15-18 across 6 windows (PR1). No layer picking, no error recovery. |
| Window open | 0.1-0.2 s; current U 0.260, target "0.26" selected, "I01_Mineral Wool_20kg/m3_0.025W/mK 80 mm will be adjusted...", "Applies to 12 panels using SIM_EXT_SLD (1 selected).", reachable 0.025-1.451, Apply off (`03-window-open`) |
| Preview | "Reached: U 0.300 W/m²K with I01_Mineral Wool... 67 mm (was 80 mm)", margin 0.000, **264 ms** per evaluation shown in the window (`04-typed-0.3`) |
| After Apply | "Applied: SIM_EXT_SLD U0.30 now has U 0.300 W/m²K (12 panels). One Undo reverts it."; check "No errors or warnings for SIM_EXT_SLD U0.30 and its 12 panels."; report saved beside the model; Copy All; Close default (`05-applied`, `reports/..._152231.txt`) |
| **Edit > ModelCheck** after Apply | 7 informational gas messages, **0 errors, no missing-material error** (`modelcheck-after-apply.txt`; the legacy apply gave an Error, PR1) |
| Saved model after Apply | 12 `WallExternal` panels on `SIM_EXT_SLD U0.30` (wool 0.067 m); `SIM_EXT_SLD` kept, unchanged, unused; 19 materials incl. `..._0.067m` |
| **One Undo** | all 12 panels back on `SIM_EXT_SLD`; 4 constructions (no unused one); 18 materials (no orphan); Undo then disabled (Apply was exactly one step) |
| Redo | 12 panels on `SIM_EXT_SLD U0.30`, material back |
| Unreachable U=0.01 | "...not reachable ... within 1-1000 mm. Best achievable: U 0.025 W/m²K at 1000 mm."; "Answered from the reachable range already calculated"; Apply disabled; Enter does nothing (`07-unreachable`) |
| Tools > U Value Calculator | new window in 0.8 s, picker open listing every construction with panel counts (`08-tools-picker-open`); typing "SIM_EXT_SLD_R" + Enter opens the roof: U 0.164, up heat flow from the 9 Roof panels (`09-tools-roof`) |
| Tools > ▾ > U Value Calculator (classic) | opens the legacy "Select Construction" only (`10`, `11`) |
| Edit > Constructions > row > Set U-value... | list closes (no edits, no prompt), window opens on `SIM_EXT_SLD_Roof` (`12`, `13`) |
| Modify in place (0.35) | `SIM_EXT_SLD U0.30` itself to wool 0.055 m on all 12 panels, same name/Guid, no new construction (`14-*`, `model-4-inplace`) |
| Selected panels only (0.35) | 1 panel on new `SIM_EXT_SLD U0.35`, 11 stay on `SIM_EXT_SLD U0.30` (`15-*`) |
| Don't assign (0.35) | `SIM_EXT_SLD U0.35` exists, no panel changed; "not assigned to any panel" (`16-*`) |
| Each mode | one Undo restores the prior state; final save after the three undos = 12 panels on `SIM_EXT_SLD U0.30`, 5 constructions |

## Found in the first acceptance pass and fixed (`evidence/.../first-pass/`)

1. The scope/result text named a construction for an unreachable target ("Creates SIM_EXT_SLD U0.01"): the new name is
   now only shown for a reached result ("Creates a new construction; ...").
2. "Last calculation 0 ms (Tas TCD)" for an answer from the cached range: now says so in words.
3. The picker's tooltip covered its first item: removed, the facts caption says typing jumps to a name.
4. A layer adjusted twice got a stacked material name (`..._0.067m_0.055m`): renamed from its base material
   (`..._0.055m`) when the base is in the library (test added).
5. "(0 panels)" for "don't assign": now "not assigned to any panel".

Driver artifacts (not product bugs): a mouse click left keyboard focus on "Details", so a later Enter re-opened Details
(WPF sends Enter to a focused button); a UIA ribbon invoke works while a modal dialog is open; the first-run snapshot
helper defaulted to the wrong model path (fixed; the final run's snapshots are taken from its own file).

## Tests (`SAM.Analytical.UI.WPF.Tests`, 20 new; full suite **1700 passed, 0 failed**)

- `UValueReportTests` (11): check covers the construction and its panels, one-line summary, glyphs, passed case,
  don't-assign wording, the check does not modify the model; report heading/provenance/stamp/change/scope/material/check
  records; in-place and don't-assign wording; report path beside the model; save never overwrites; unsaved model and
  missing folder refuse with a Copy All hint.
- `SetUValueWindowTests` (8, STA, `WpfCollection`): opened from panels (construction, current U, pre-fill, scope, layer
  sentence, Apply off, Advanced collapsed); opened from Tools (no construction, nothing to apply); choosing in the
  picker; reachable/unreachable preview and Apply/IsDefault; Apply changes the model once, shows the check, saves the
  report beside the model, then only Close/Copy All; unsaved model offers only Copy All; Advanced options reach the
  view-model; at the smallest size the action bar is inside the window.
- `SetUValueTests` + `UValueViewModelTests`: material named from its base when adjusted twice; unreachable target names
  no construction.

## Decisions and assumptions

- "Next to the model's output folder" = the model's own folder (SAM's default simulation output folder,
  `Simulate.cs`); a timestamped name so each Apply keeps its own report.
- Inline busy state, no progress dialog: an evaluation is ~0.25 s and Apply (incl. Tas `UpdateThermalParameters`) ~0.4 s
  on this model; a wait cursor covers Apply.
- The split button keeps the old x:Name (`RibbonButton_ThermalTransmittanceCalculator`); its menu-item click bubbles to
  the button's Click, so the handler ignores clicks from a `RibbonMenuItem`.
- The Constructions list hands over by closing (Cancel semantics unless the user saves): two open editors of the same
  constructions would otherwise overwrite each other.

## Not covered / risks

- The Edit > Constructions **unsaved-edits prompt** (Yes/No/Cancel) was not driven in the real app (the handover without
  edits was).
- "Tas unavailable" was not provoked in the real app (unit tests only, as PR1).
- Copy All's clipboard was not read back in the real app; its text is the saved report (window test).
- Evidence uses a folder copy of `SAM_UI\build`, not an installer.
- Mixed wall/roof panels on one construction get the majority basis with a warning (PR2a).

## Files changed

- New: `WPF/SAM.Analytical.UI.WPF/Windows/SetUValueWindow.xaml(.cs)`, `Modify/OpenSetUValueWindow.cs`,
  `Modify/SaveUValueChangeReport.cs`, `Query/UValueCheck.cs`, `Query/UValueChangeReport.cs`,
  `Classes/UValue/UValueCheckSummary.cs`, `SAM_UI/SAM.Analytical.UI/Classes/EventArgs/SetUValueRequestedEventArgs.cs`.
- Changed: `Windows/AnalyticalWindow.xaml(.cs)` (context-menu item, Tools split button), `Modify/EditConstructions.cs`
  (hand-over), `SAM_UI/SAM.Analytical.UI/Windows/ConstructionLibraryWindow.xaml(.cs)` (opt-in button + event),
  `Classes/UValue/UValueViewModel.cs` (no name for unreachable targets), `Modify/SetUValue.cs` (material base name).
- Tests: `UValueReportTests.cs`, `SetUValueWindowTests.cs` (new), `SetUValueTests.cs`, `UValueViewModelTests.cs`.
- Docs: this record; `evidence/uvalue-pr2b-2026-10-01/` (final run, first pass, new driver scripts; the PR1 scripts in
  `evidence/uvalue-workflow-2026-10-01/scripts/` are reused).

## Next step

Wait for green CI and explicit merge approval; then the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q3`. Then PR3
(glazing selection) per the plan; PR4 (U-value / Used-by columns, restyling, and whether the classic calculator stays)
remains optional.
