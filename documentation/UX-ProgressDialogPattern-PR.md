# UX: one SAM progress-dialog pattern for reporting/export progress — PR record

Branch `feature/progress-dialog-pattern-2026-09-28`, from `sow/2026-Q3` (`4da598b9`). Updated with
`6b49c8c6` (AGENTS.md convention) and `52217c32` (SAM_UI#134 PR3C closeout); both merged without conflicts.
Sibling heads used: SAM `bc85ba61`, SAM_Systems `fbef48f`, SAM_Tas `5753ad2e`. SAM_Deploy is not touched.

**Status:** implemented, tested, visually exercised in the dev build. PR open against `sow/2026-Q3`.
`PROJECT_PROGRESS.md` is not changed here: it gets a closeout entry after merge (AGENTS.md).

## Scope

UX, refactoring and documentation only. There are no changes to:

- reporting semantics, the batch algorithm, file names, prompts, logs or cancellation points;
- Room Data Sheet content;
- engineering logic;
- deployment.

The pattern is documented in [`ProgressDialogPattern.md`](ProgressDialogPattern.md). Its reference implementation is
the Part O progress window.

## Part O reference located

- `Windows/PartOProgressWindow.xaml(.cs)`: the window.
- `Classes/PartO/PartOProgressState.cs`: stages, times, real-count percentage, note text.
- `Classes/PartO/PartOProgressHost.cs`: own UI thread, token, `AllowCancel`.
- `Themes/PartOStyles.xaml`: Part O palette and buttons. Not used by the progress window, which had inline values.
- The strings in the brief ("Build & Run Mixed Design", "Materialise the mixed model from the baseline", "TAS
  simulation (full year) and TM59 assessment") are the stage list `RunPartOMixedDesignCommand` passes to
  `PartOProgressHost`. "Elapsed" and "Cancel takes effect at the next safe point" come from `PartOProgressState`.

## Work done

1. **Extracted the look into neutral shared resources.**
   - `WPF/SAM.Core.UI.WPF/Themes/ProgressStyles.xaml` holds the `SAM.Progress.*` styles and the stage-row template,
     with the Part O window's values unchanged.
   - `WPF/SAM.Core.UI.WPF/Classes/ProgressStageRow.cs` is the former private `PartOProgressWindow.Row`.
   - `Create.ProgressStageRows(PartOProgressState)` builds the rows.
   - These live in SAM.Core.UI.WPF, the lowest WPF layer, so any SAM WPF project can use them.
2. **`PartOProgressWindow`** now draws with them. Element names, layout values and behaviour are unchanged, and
   every Part O progress test passes unchanged.
3. **`SpaceReportPdfBatchWindow`** follows the pattern:
   - heading, run summary, two stages with right-aligned times and a live "n of N" count;
   - the document in hand, a determinate bar with a real percentage, and the elapsed time;
   - the note beside Cancel, in the Part O position;
   - final states: exported / exported with failures / cancelled / could not start / stopped;
   - it renders on a 500 ms timer, like Part O;
   - the form, prompts, batch calls, cancel, close-while-running and summary text are unchanged.
   - When the existing-PDFs question is cancelled, the progress still disappears and the form is as before.
4. **Print Room Data Sheets (ribbon)** shows its four stages in the shared style (`PartOProgressWindow`; title
   "Print RDS", no percentage, not cancellable). **Style only: the execution and threading are the old window's.**
   The window is created on the calling (UI) thread when the first stage starts, shown modelessly with no owner,
   not topmost, not in the taskbar and centred on screen. On each stage it is activated and the dispatcher is
   pumped once so it repaints (the old `ProgressWindow.Update`), and it closes when the work returns or throws.
   - `SAM.Analytical.UI` `PrintRoomDataSheets` gained an overload with an optional stage callback. The stages are
     announced at the same points as the old `Update` calls. With no callback, the old window and path are
     unchanged, so the Simulate workflow and Grasshopper callers are untouched.
   - Review round (owner): the first version hosted this window on its own thread with `PartOProgressHost`, which
     made it topmost. That was an unverified behaviour change, since Excel is unavailable here, so it was reverted.
     `PartOProgressHost` is now identical to the base branch.
5. **Audit** of every reporting / export / print progress surface, classified: standardise now / already
   consistent / not applicable. See `ProgressDialogPattern.md`.

## Files

| Area | Files |
|---|---|
| New shared | `WPF/SAM.Core.UI.WPF/Themes/ProgressStyles.xaml`, `WPF/SAM.Core.UI.WPF/Classes/ProgressStageRow.cs`, `WPF/SAM.Analytical.UI.WPF/Create/ProgressStageRows.cs` |
| Part O reference | `Windows/PartOProgressWindow.xaml(.cs)` (`PartOProgressHost` unchanged) |
| Space reports | `Windows/SpaceReportPdfBatchWindow.xaml(.cs)` |
| Print RDS | `SAM_UI/SAM.Analytical.UI/Modify/PrintRoomDataSheets.cs` (stage-callback overload), `Modify/PrintRoomDataSheetsWithProgress.cs` (new), `Windows/AnalyticalWindow.xaml.cs` (ribbon handler) |
| Tests | `ProgressDialogPatternTests.cs` (9), `ProgressDialogPatternEvidenceHarness.cs` (env-gated) |
| Docs and evidence | `documentation/ProgressDialogPattern.md`, this record, `Reporting-SpaceReportPdfBatch.md` (window description), `documentation/evidence/progress-dialog-pattern/` |

## Validation

- `SAM_UI.sln` Release and Debug: 0 errors.
- WPF tests: **1395/1395** Release on the merged branch (includes SAM_UI#134's tests and the 9 new ones; one
  pins the Print RDS window's title, not topmost, not in the taskbar, no owner, centred, no Cancel, the note text
  and the shared styles).
  All existing `SpaceReportPdfBatch*` and `PartOProgress*` tests pass unchanged.
- **Deterministic renders** (`evidence/progress-dialog-pattern/render/`): the real window running the real batch on
  `bridge_peaks.sam`, with a stand-in renderer gated for mid-run states. States: Part O reference, Print RDS, batch
  form / preparing / writing / cancel requested / cancelled / completed / completed with failures.
  - Re-run: set env `SAM_PROGRESS_EVIDENCE_OUT=<folder>` (optional `SAM_PROGRESS_EVIDENCE_MODEL=<x.sam>`), then
    `dotnet test -c Release --no-build --filter FullyQualifiedName~ProgressDialogPatternEvidenceHarness`.
- **Dev app, UIA** (`build\SAM Analytical.exe`, 4,995-Space `bridge_x555.sam`; `evidence/.../app/`,
  `drive-log.txt`, `scripts/drive.ps1.txt`):
  - window 520 × 302 px, as designed;
  - both reports: live stage rows, count, percentage and elapsed time; Cancel → stopped and Export re-enabled in
    **0.06 s**; "Export cancelled", "stopped after 884 of 9,990";
  - one report: existing-PDF question → Yes; **4,995 PDFs in 49 s** (PR2F-2: about 50 s per 5k);
    "Space reports exported", 100%;
  - one locked PDF: "exported, with failures", Failed 1 with the reason; no `.tmp` files; logs written.
- **Print RDS, dev app** (`rds-log.txt`, `scripts/rds.ps1.txt`):
  - the ribbon opened the "Print RDS" window at 0.08 s and it closed at 0.27 s;
  - Win32 checks on the live window: **on the main window's UI thread** (same thread id), **not topmost** (no
    `WS_EX_TOPMOST`), and owned only by WPF's hidden `HwndWrapper`, which keeps a `ShowInTaskbar=False` window with no
    `Owner` out of the taskbar. Its owner is not the main window, as with the old window;
  - Excel is not installed on this VM, so the command ends at its Excel step exactly as before: no dialog, and the
    app keeps running;
  - the dev build found the RDS template in `%APPDATA%\SAM\resources`;
  - the window's look is shown by the render `02-rds-printing.png`.

## Decisions

- The shared pieces are **resources + row + factory**, not a UserControl. The Part O tests find controls by name on
  the window, and a UserControl would give them their own name scope. Styles keep the names and remove the copied
  markup.
- The **stage model stays `PartOProgressState`**: it is pure and already tested. Renaming the Part O types would
  touch every Part O command, so it is left as a recommended follow-up.
- **Cancelled** shows the write stage as ✕ "Did not complete", with "stopped after n of N". The bar ends at the
  result's own count.
- **Render on a timer.** The first attempt rendered on every progress tick and made Cancel answer in about 10 s in
  the 5k UI run. It was fixed before the evidence runs.

## Risks and open items

- Print RDS with Excel present was not exercised on this VM, because Excel is missing. The work path, stage
  points, thread and window behaviour are the old ones. Only the window's content and style differ: the window
  is 520 px wide rather than 420, and shows a stage list rather than a caption and step bar. As before, the window
  cannot repaint while Excel holds the UI thread.
- Model export and the generic `ProgressBarWindow` are not standardised (see the doc's next steps).

## Next step

Review → final CI → merge. After merge, add the `PROJECT_PROGRESS.md` closeout entry on `sow/2026-Q3` with the
merge SHA. Deploying through SAM_Deploy is owner-led and separate.
